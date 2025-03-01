[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]

namespace Bloqqer.Test.Integration;

public class IntegrationTestFixture
{
    // Test database and db user account.
    // This exists only in the context of a test run, 
    // so no need to keep passwords as a secret
    private readonly IContainer? DbTestContainer;
    private const string DbName = "Bloqqer.IntegrationTests";
    private const string DbUser = "sa";
    private const string DbPassword = "$trongP4ssword";
    private const int DbPort = 1433;

    // User account for our test user.
    // This exists only in the context of a test run, 
    // so no need to keep passwords as a secret
    public static string ApiTestUserEmail = $"user_{Guid.NewGuid()}@bloqqer.net";
    public static string ApiTestUserName = "integration.test.user";
    public static string ApiTestUserPassword = "Pa55w0rd123";

    /// <summary>
    /// Initializes an instance of the IntegrationTestFixture class.
    /// This class is initialized only once at the beginning of a test run,
    /// and is shared across all tests in the assembly.
    /// </summary>
    public IntegrationTestFixture()
    {
        // Create and start the test database container
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

        // Create an in-memory Bloqqer.Api web application via WebApplicationFactory
        // and configure EF Core to connect to the test container database
        WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => 
                    services.AddDbContext<BloqqerDbContext>(options => 
                        options.UseSqlServer(dbConnectionString))));

        // Apply migrations
        using var dbContext = ScopedServiceProvider.GetRequiredService<BloqqerDbContext>();
        dbContext.Database.Migrate();

        // Register & login the test user to get a JWT
        var authService = ScopedServiceProvider.GetRequiredService<IAuthService>();
        var confirmationCode = authService.RegisterAsync(new(ApiTestUserEmail)).Result;
        var registeredUser = authService.ConfirmRegistrationAsync(new(confirmationCode, ApiTestUserName, ApiTestUserPassword)).Result;
        var (_, Jwt) = authService.LoginAsync(new(registeredUser.Email, ApiTestUserPassword)).Result;

        // Set the JWT for the BloqqerApiClient in order to make authenticated requests to protected APIs
        BloqqerApiClient = WebApplicationFactory.CreateClient();
        BloqqerApiClient.DefaultRequestHeaders.Authorization = new("Bearer", Jwt);
    }

    /// <summary>
    /// HTTP client for calling Bloqqer.Api endpoints in tests.
    /// </summary>
    public HttpClient BloqqerApiClient { get; init; }

    /// <summary>
    /// Factory for creating instances of Bloqqer.Api as an in-memory web application.
    /// The generic type parameter <Program> references the Program class in Bloqqer.Api
    /// </summary>
    public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

    /// <summary>
    /// Service provider that can be used in tests to resolve a scoped service from the Bloqqer.Api DI container.
    /// </summary>
    public IServiceProvider ScopedServiceProvider => WebApplicationFactory.Services.CreateScope().ServiceProvider;

    /// <summary>
    /// Get an instance of BloqqerApiClient configured with the specified mock service
    /// that replaces the original service in the DI container. The client is scoped to the
    /// test that creates it and will not interfere with other tests running at the same time.
    /// </summary>
    public HttpClient CreateClientWithMockServices<TService>(TService mockService) where TService : class
    {
        var client = WebApplicationFactory
            .WithWebHostBuilder(builder => builder
            .ConfigureTestServices(services =>
            {
                var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
                if (serviceDescriptor is not null) services.Remove(serviceDescriptor);
                services.AddTransient(_ => mockService);
            }))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization = BloqqerApiClient.DefaultRequestHeaders.Authorization;

        return client;
    }
    
    /// <summary>
    /// Clean up resources after all tests have ran.
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