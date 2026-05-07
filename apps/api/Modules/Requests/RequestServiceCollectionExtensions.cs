using LineCom.Api.Modules.Requests.Repositories;
using LineCom.Api.Modules.Requests.Services;

namespace LineCom.Api.Modules.Requests;

public static class RequestServiceCollectionExtensions
{
    public static IServiceCollection AddRequestModule(this IServiceCollection services)
    {
        services.AddScoped<IRequestNumberGenerator, RequestNumberGenerator>();
        services.AddScoped<IRequestNumberRepository, DapperRequestNumberRepository>();
        services.AddScoped<ICustomerRequestService, CustomerRequestService>();
        services.AddScoped<ICustomerRequestRepository, DapperCustomerRequestRepository>();
        services.AddSingleton<IRequestReferenceData, RequestReferenceData>();

        return services;
    }
}
