using System.ComponentModel.DataAnnotations;
using EventManager.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);
            ThreadPool.GetAvailableThreads(out int availableWorker, out int availableIo);

            Console.WriteLine($"Рабочие потоки: {maxWorker - availableWorker}/{maxWorker}");
            Console.WriteLine($"I/O потоки: {maxIo - availableIo}/{maxIo}"); 
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }
    
    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);
            
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = MapStatusCode(ex);
        
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = ex.Message
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }
    
    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException ve => StatusCodes.Status400BadRequest,
            NotFoundException nf => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
    
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
    } 
}