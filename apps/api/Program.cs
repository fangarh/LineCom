using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Infrastructure.Hosting;
using LineCom.Api.Modules.Account;
using LineCom.Api.Modules.Auth;
using LineCom.Api.Modules.Catalog;
using LineCom.Api.Modules.Requests;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (DevelopmentLoggingPolicy.ShouldUseDevelopmentConsoleLogging(builder.Environment))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(ReverseProxyForwardingPolicy.Configure);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuthRateLimiting();
builder.Services.AddAuthModule(builder.Environment);
builder.Services.AddAccountModule();
builder.Services.AddCatalogModule();
builder.Services.AddRequestModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseForwardedHeaders();

if (HttpsRedirectionPolicy.ShouldUseHttpsRedirection(app.Environment))
{
    app.UseHttpsRedirection();
}

app.UseLocalStorageStaticFiles(builder.Configuration);

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
