using LineCom.Api.Modules.Account.Repositories;
using LineCom.Api.Modules.Account.Services;

namespace LineCom.Api.Modules.Account;

public static class AccountServiceCollectionExtensions
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountProfileService, AccountProfileService>();
        services.AddScoped<IAccountProfileRepository, DapperAccountProfileRepository>();

        return services;
    }
}
