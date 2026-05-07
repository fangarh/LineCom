using LineCom.Api.Modules.Account;
using LineCom.Api.Modules.Account.Repositories;
using LineCom.Api.Modules.Account.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LineCom.Api.Tests.Modules.Account;

public sealed class AccountModuleRegistrationTests
{
    [Fact]
    public void AddAccountModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddAccountModule();

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddAccountModule_RegistersAccountProfileServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAccountModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IAccountProfileService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(AccountProfileService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddAccountModule_RegistersAccountProfileRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        services.AddAccountModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IAccountProfileRepository));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperAccountProfileRepository), descriptor.ImplementationType);
    }
}
