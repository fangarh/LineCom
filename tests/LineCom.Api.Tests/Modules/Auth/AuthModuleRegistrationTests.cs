using LineCom.Api.Modules.Auth;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LineCom.Api.Tests.Modules.Auth;

public sealed class AuthModuleRegistrationTests
{
    [Fact]
    public void AddAuthModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddAuthModule();

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddAuthModule_RegistersCustomerRegistrationServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(ICustomerRegistrationService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(CustomerRegistrationService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersUserRegistrationRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IUserRegistrationRepository));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperUserRegistrationRepository), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersPasswordHasherAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IPasswordHasher));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(Pbkdf2PasswordHasher), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersAuthSessionServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IAuthSessionService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(CookieAuthSessionService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersCustomerLoginServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(ICustomerLoginService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(CustomerLoginService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersUserLoginRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IUserLoginRepository));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperUserLoginRepository), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_RegistersAuthCurrentUserServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuthModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IAuthCurrentUserService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(AuthCurrentUserService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAuthModule_UsesSecureCookiePolicyInProduction()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthModule(new TestWebHostEnvironment { EnvironmentName = Environments.Production });

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
    }

    [Fact]
    public void AddAuthModule_KeepsSameAsRequestCookiePolicyOutsideProduction()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAuthModule(new TestWebHostEnvironment { EnvironmentName = Environments.Development });

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "LineCom.Api.Tests";

        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
