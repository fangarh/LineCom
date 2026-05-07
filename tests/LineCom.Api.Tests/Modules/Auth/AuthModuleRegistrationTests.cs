using LineCom.Api.Modules.Auth;
using LineCom.Api.Modules.Auth.Repositories;
using LineCom.Api.Modules.Auth.Services;
using Microsoft.Extensions.DependencyInjection;

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
}
