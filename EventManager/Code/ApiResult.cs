using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Code;

public class ApiResult<T> : ApiBaseResult, IActionResult
{
    public required T Data { get; set; }
    
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var objectResult = new ObjectResult(this)
        {
            StatusCode = (int)StatusCode
        };
        
        await objectResult.ExecuteResultAsync(context);
    }
}

public class ApiResult : ApiBaseResult { }


public class ApiBaseResult
{
   
    public required bool Success { get; set; }
   
    public required HttpStatusCode StatusCode { get; set; }
   
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
   
    public required string Message { get; set; }
} 