using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Events.Api.Security;

public sealed class InternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration["InternalApi:Key"];
        if (string.IsNullOrEmpty(expected)
            || !context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided)
            || provided != expected)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
