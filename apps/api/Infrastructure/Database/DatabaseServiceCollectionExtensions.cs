using Npgsql;

namespace LineCom.Api.Infrastructure.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is not configured.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }
}
