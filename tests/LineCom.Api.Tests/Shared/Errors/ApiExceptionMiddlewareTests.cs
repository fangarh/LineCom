using System.Net;
using System.Text.Json;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace LineCom.Api.Tests.Shared.Errors;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNext_WhenNoExceptionThrown()
    {
        var context = CreateContext();
        var wasCalled = false;
        var middleware = new ApiExceptionMiddleware(
            nextContext =>
            {
                wasCalled = true;
                nextContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(wasCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_MapsApiException_ToConfiguredStatusCode()
    {
        var context = CreateContext();
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
    public async Task InvokeAsync_MapsApiException_ToBadRequest()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new ApiException("catalog.invalid_filter", "Некорректный параметр фильтра.", StatusCodes.Status400BadRequest),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var body = await ReadErrorAsync(context);
        Assert.Equal("catalog.invalid_filter", body.Code);
        Assert.Equal("Некорректный параметр фильтра.", body.Message);
    }

    [Fact]
    public async Task InvokeAsync_MapsUnhandledException_ToInternalError()
    {
        var context = CreateContext();
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

    [Fact]
    public async Task InvokeAsync_MapsUnhandledException_WithoutLeakingExceptionMessage()
    {
        var context = CreateContext();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Database password leaked in exception."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Database password leaked", responseBody);
        Assert.DoesNotContain(nameof(InvalidOperationException), responseBody);
    }

    [Fact]
    public async Task InvokeAsync_Throws_WhenResponseAlreadyStarted()
    {
        var context = CreateStartedContext();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new ApiException("catalog.product_not_found", "Товар не найден.", StatusCodes.Status404NotFound),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        Assert.Equal("Cannot write API error response because the response has already started.", exception.Message);
    }

    [Fact]
    public void ApiException_StoresCodeMessageAndStatusCode()
    {
        var exception = new ApiException(
            "catalog.product_not_found",
            "Товар не найден.",
            StatusCodes.Status404NotFound);

        Assert.Equal("catalog.product_not_found", exception.Code);
        Assert.Equal("Товар не найден.", exception.Message);
        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static DefaultHttpContext CreateStartedContext()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        return context;
    }

    private static async Task<ApiErrorResponse> ReadErrorAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Assert.IsType<ApiErrorResponse>(body);
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }
}
