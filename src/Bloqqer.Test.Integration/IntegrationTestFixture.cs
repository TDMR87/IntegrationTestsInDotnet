[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]

namespace Bloqqer.Test.Integration;

public class IntegrationTestFixture
{
    private readonly IContainer? DbTestContainer;
    private const string DbName = "Bloqqer.IntegrationTests";
    private const string DbUser = "sa";
    private const string DbPassword = "$trongP4ssword";
    private const int DbPort = 1433;

    private const string ApiTestUserEmail = "integration.test.user@bloqqer.net";
    private const string ApiTestUserName = "integration.test.user";
    private const string ApiTestUserPassword = "Pa55w0rd123";

    public IntegrationTestFixture()
    {
        // Create a test container database
        DbTestContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPortBinding(DbPort, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQLCMDUSER", DbUser)
            .WithEnvironment("SQLCMDPASSWORD", DbPassword)
            .WithEnvironment("MSSQL_SA_PASSWORD", DbPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(DbPort))
            .Build();

        DbTestContainer.StartAsync().Wait();

        var dbConnectionString =
            $"Server={DbTestContainer.Hostname},{DbTestContainer.GetMappedPublicPort(DbPort)};" +
            $"Database={DbName};User Id={DbUser};" +
            $"Password={DbPassword};TrustServerCertificate=True";

        // Create an in-memory Bloqqer.Api web application
        // and configure EF Core to connect to the test container database
        WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => 
                    services.AddDbContext<BloqqerDbContext>(options => 
                        options.UseSqlServer(dbConnectionString))));

        // Apply migrations to the database
        using var dbContext = ScopedServiceProvider.GetRequiredService<BloqqerDbContext>();
        dbContext.Database.Migrate();

        // Register & login the test user
        var authService = ScopedServiceProvider.GetRequiredService<IAuthService>();
        var confirmationCode = authService.RegisterAsync(new(ApiTestUserEmail)).Result;
        var registeredUser = authService.ConfirmRegistrationAsync(new(confirmationCode, ApiTestUserName, ApiTestUserPassword)).Result;
        var (_, Jwt) = authService.LoginAsync(new(registeredUser.Email, ApiTestUserPassword)).Result;

        // Set the JWT for the HttpClient
        BloqqerApiClient = WebApplicationFactory.CreateClient();
        BloqqerApiClient.DefaultRequestHeaders.Authorization = new("Bearer", Jwt);
    }

    /// <summary>
    /// HTTP client for calling Bloqqer.Api endpoints.
    /// </summary>
    public HttpClient BloqqerApiClient { get; init; }

    /// <summary>
    /// WebApplicationFactory for creating an instance of Bloqqer.Api as an in-memory web application.
    /// </summary>
    public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

    /// <summary>
    /// Service provider for resolving a scoped service from the Bloqqer.Api DI container.
    /// </summary>
    public IServiceProvider ScopedServiceProvider => WebApplicationFactory.Services.CreateScope().ServiceProvider;

    /// <summary>
    /// Creates an HTTP client that replaces services in the Bloqqer.Api 
    /// DI container with the provided implementations.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="mockServices"></param>
    /// <returns></returns>
    public HttpClient CreateClientWithMockServices<TService>(
        TService mockService) where TService : class
    {
        var client = WebApplicationFactory.WithWebHostBuilder(builder => 
            builder.ConfigureTestServices(services =>
            {
                var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
                if (serviceDescriptor is not null) services.Remove(serviceDescriptor);
                services.AddTransient(_ => mockService);
            }))
        .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            BloqqerApiClient.DefaultRequestHeaders.Authorization;

        return client;
    }

    /// <summary>
    /// Clean up resources and tear down the test container databases.
    /// </summary>
    public async void DisposeAsync()
    {
        if (DbTestContainer is not null)
        {
            await DbTestContainer.StopAsync();
            await DbTestContainer.DisposeAsync();
        }

        BloqqerApiClient?.Dispose();
        WebApplicationFactory?.Dispose();
    }
}