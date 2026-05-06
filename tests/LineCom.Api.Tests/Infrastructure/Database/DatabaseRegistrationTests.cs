using LineCom.Api.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class DatabaseRegistrationTests
{
    [Fact]
    public void AddDatabase_Throws_WhenConnectionStringMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDatabase(configuration));

        Assert.Equal("Connection string 'Default' is not configured.", exception.Message);
    }

    [Fact]
    public void AddDatabase_RegistersDataSourceAndConnectionFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddDatabase(configuration);

        using var provider = services.BuildServiceProvider();
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        var factory = provider.GetRequiredService<IDbConnectionFactory>();

        Assert.NotNull(dataSource);
        Assert.NotNull(factory);
    }
}
