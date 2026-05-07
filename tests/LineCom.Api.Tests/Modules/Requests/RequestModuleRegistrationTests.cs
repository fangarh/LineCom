using LineCom.Api.Modules.Requests;
using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Modules.Requests.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class RequestModuleRegistrationTests
{
    [Fact]
    public void AddRequestModule_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddRequestModule();

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddRequestModule_RegistersRequestNumberGeneratorAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRequestModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IRequestNumberGenerator));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(RequestNumberGenerator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRequestModule_RegistersRequestNumberRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRequestModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IRequestNumberRepository));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperRequestNumberRepository), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRequestModule_RegistersCustomerRequestServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRequestModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(ICustomerRequestService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(CustomerRequestService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRequestModule_RegistersCustomerRequestRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        services.AddRequestModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(ICustomerRequestRepository));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DapperCustomerRequestRepository), descriptor.ImplementationType);
    }

    [Fact]
    public void AddRequestModule_RegistersRequestReferenceDataAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddRequestModule();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IRequestReferenceData));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(RequestReferenceData), descriptor.ImplementationType);
    }
}
