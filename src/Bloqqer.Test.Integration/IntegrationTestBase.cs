namespace Bloqqer.Test.Integration;

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

    protected User TestUser => DbContext.Users.FirstOrDefault(
        user => user.Email == "integration.test.user@bloqqer.net") 
        ?? throw new Exception("Integration test user not found");

    protected static readonly JsonSerializerOptions DisallowUnmappedMembers = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = true
    };
}
