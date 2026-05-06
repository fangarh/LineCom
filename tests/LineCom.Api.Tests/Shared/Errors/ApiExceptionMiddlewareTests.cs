using System.Net;
using System.Text.Json;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LineCom.Api.Tests.Shared.Errors;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_MapsApiException_ToConfiguredStatusCode()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new ApiException("catalog.not_found", "Товар не найден.", StatusCodes.Status404NotFound),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.Equal("catalog.not_found", body.Code);
        Assert.Equal("Товар не найден.", body.Message);
    }

    [Fact]
    public async Task InvokeAsync_MapsUnhandledException_ToInternalError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Database password leaked in exception."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.Equal("internal_error", body.Code);
        Assert.Equal("Внутренняя ошибка сервера.", body.Message);
    }
}
