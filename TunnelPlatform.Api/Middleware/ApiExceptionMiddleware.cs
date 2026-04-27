using System.Text.Json;

namespace TunnelPlatform.Api.Middleware;

/// <summary>
/// 统一异常处理中间件。
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, int statusCode, Exception exception)
    {
        _logger.LogError(exception, "接口处理失败，路径：{Path}", context.Request.Path);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            message = exception.Message,
            statusCode,
            traceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

