using Microsoft.AspNetCore.Mvc.Testing;

namespace Bloqqer.Test.Integration;

/// <summary>
/// Base class for each test class. Each individual test method will
/// be instantiated with it's own scoped services.
/// </summary>
/// <param name="Fixture"></param>
public abstract class IntegrationTestBase(IntegrationTestFixture Fixture)
{
    protected HttpClient BloqqerApiClient => Fixture.BloqqerApiClient;

    protected BloqqerDbContext DbContext { get; set; } = 
        Fixture.ScopedServiceProvider.GetRequiredService<BloqqerDbContext>();

    protected IUserService UserService => 
        Fixture.ScopedServiceProvider.GetRequiredService<IUserService>();

    protected IArticleService ArticleService => 
        Fixture.ScopedServiceProvider.GetRequiredService<IArticleService>();

    protected IAuthService AuthService =>
        Fixture.ScopedServiceProvider.GetRequiredService<IAuthService>();

    public IConfiguration Configuration => 
        Fixture.ScopedServiceProvider.GetRequiredService<IConfiguration>();

    /// <summary>
    /// Returns the currently signed in integration test user
    /// </summary>
    protected User TestUser => DbContext.Users.FirstOrDefault(
        user => user.Email == IntegrationTestFixture.ApiTestUserEmail) 
        ?? throw new Exception("Integration test user not found");


    protected static readonly JsonSerializerOptions BloqqerJsonSerializerOptions = new()
    {
        // Fail deserialization if members do not match.
        // This will prevent us from receiving wrong data from an API response
        // and regarding it as successfull result.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

        // Ignore case when deserializing JSON to support PascalCase and camelCase
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Get an instance of BloqqerApiClient configured with the specified mock service
    /// that replaces the original service in the DI container. The client is scoped to the
    /// test that creates it and will not interfere with other tests running at the same time.
    /// </summary>
    public HttpClient CreateClientWithMockServices<TService>(TService mockService) where TService : class
    {
        var client = Fixture.WebApplicationFactory
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
}
