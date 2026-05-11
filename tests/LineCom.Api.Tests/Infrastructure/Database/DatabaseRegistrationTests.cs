using LineCom.Api.Infrastructure.Database;
using LineCom.Api.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LineCom.Api.Tests.Infrastructure.Database;

public sealed class DatabaseRegistrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddDatabase_Throws_WhenConnectionStringMissingOrBlank(string? connectionString)
    {
        var configurationBuilder = new ConfigurationBuilder();
        if (connectionString is not null)
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            });
        }

        var configuration = configurationBuilder.Build();
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

    [Fact]
    public void AddDatabase_ReturnsSameServiceCollection()
    {
        var configuration = CreateConfigurationWithConnectionString();
        var services = new ServiceCollection();

        var returnedServices = services.AddDatabase(configuration);

        Assert.Same(services, returnedServices);
    }

    [Fact]
    public void AddDatabase_RegistersDataSourceAsSingleton()
    {
        var configuration = CreateConfigurationWithConnectionString();
        var services = new ServiceCollection();

        services.AddDatabase(configuration);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<NpgsqlDataSource>();
        var second = provider.GetRequiredService<NpgsqlDataSource>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddDatabase_RegistersConnectionFactoryAsScoped()
    {
        var configuration = CreateConfigurationWithConnectionString();
        var services = new ServiceCollection();

        services.AddDatabase(configuration);

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstInScope = firstScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var secondInScope = firstScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var otherScope = secondScope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        Assert.Same(firstInScope, secondInScope);
        Assert.NotSame(firstInScope, otherScope);
    }

    [Fact]
    public void AddDatabase_RegistersLocalStoredFileWriterAndOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom",
                ["Storage:RootPath"] = "D:\\linecom-storage"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Directory.GetCurrentDirectory()));

        services.AddDatabase(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ILocalStoredFileWriter>();
        var options = provider.GetRequiredService<IOptions<LocalStoredFileOptions>>();

        Assert.IsType<LocalStoredFileWriter>(writer);
        Assert.Equal("D:\\linecom-storage", options.Value.RootPath);
    }

    [Fact]
    public async Task OpenConnectionAsync_ThrowsOperationCanceled_WhenTokenAlreadyCanceled()
    {
        await using var dataSource = new NpgsqlDataSourceBuilder(
                "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom")
            .Build();
        var factory = new NpgsqlConnectionFactory(dataSource);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await factory.OpenConnectionAsync(cancellationTokenSource.Token));
    }

    private static IConfiguration CreateConfigurationWithConnectionString()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=linecom;Username=linecom;Password=linecom"
            })
            .Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "LineCom.Api.Tests";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
