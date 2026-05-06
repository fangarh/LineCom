using System.Text.Json;

namespace LineCom.Api.Shared.Errors;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        catch (ApiException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "Внутренняя ошибка сервера.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write API error response because the response has already started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse(code, message);
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions, context.RequestAborted);
    }
}
