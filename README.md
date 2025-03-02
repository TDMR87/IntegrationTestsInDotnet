# Integration Tests In .NET

This is sample project showcasing one of the ways to implement integration tests for a .NET web api project.

## Goals
The following are the high level goals we want to accomplish:

- **A Real database**: Execute tests against an instance of a database that is equivalent to the production SQL Server database. That means no in-memory databases, no SQLite, etc.

- **A Shared database**: Instantiate a single, shared database for all of the tests to use. Do not tear down the database for each test, and do not run tests in a sterile, isolated database.

- **Test Parallelism**: Run tests in parallel, even though a single database instance is used. Avoid issues with EF Core DbContext not being thread safe and avoid tests that are running in parallel affecting each other.

- **Real HTTP Requests**: Test API endpoints using real HTTP requests, sending data in request body, requests going through the real API middleware pipeline, request parameters going through real parameter binding and serialization etc.

- **Authorized endpoints**: Test also the APIs that require authorization using the authorization mechanics of the API.

- **Minimal Mocking**: Try to keep mocking SUTs & dependencies as rare as possible, but have the ability to do so when needed.

- **Minimal configuration**: Avoid having to configure the test environment specifically for testing. Utilize the production code and implementations as much as possible, including the DI container and all the configurations in the WebApplicationBuilder of the production app.

- **Domain-driven test setup**: Avoid direct database manipulation in test setup as much as possible. Use existing services to arrange & setup the initial test state where possible. Often you see test state being setup by manipulating the database directly with DbContext. This works, of course, but does not convey domain knowledge very well. Using services to setup tests requires us to rely on the production code implementations, which in turn makes it clearer to the reader what parts in our app actually need to contribute in the process.

_In short_, we want our tests to execute scenarios as close as possible to the real world, in regards to parallelism, multi-threading, configuration, authorization etc. We also want to have a single database behind all tests, which models the real world more closely but also gives us test execution speed.

## About the solution
The solution in this project is a backend API for an imaginary blogging site called _Bloqqer_, where users can register, login and create/read/update/delete written articles. For the sake of simplicity, there exists only two main domain entities in this project, _Article_ and _User_. 

## The test project

### The fixture

The heart of our test project is the ```IntegrationTestFixture.cs``` class. This is an ```AssemblyFixture```. Newly introduced in xUnit 3, you can share a single instance of a fixture class among all the test classes in your test assembly with just one line of code. 

```csharp
[assembly: AssemblyFixture(typeof(IntegrationTestFixture))]
```

What's even more important, is that all our  tests contained in this _AssemblyFixture_ are [executed in parallel](https://goog.ecom) by default. This is exactly what we'd want to be able to execute our tests faster.

Because the IntegrationTestFixture class is instantiated once at the beginning of a test run, we can use it to setup resources that are share among all the tests. Let's start with the database.

### Database

We want to create a single database to run our tests against. Why a shared database and not a clean database for each test? Tearing down and creating a fresh new database for each test would create a lot of overhead and slow down our test execution. Furthermore, that is not how your system operates in production. In production, your system handles multiple requests in parallel, without it being an issue. Why should tests be required to have an unnaturally sterile environment? 

Running the tests in a shared database instance instead of a clean, isolated database, models the real world and makes the tests more reliable, since you can catch issues with concurrency better. If your tests fail because there already exists data in the database, then the test itself should be written in a way that makes the test more isolated. The environment should not be made isolated just so that your test can pass. 

> To achieve this, we have to keep in mind a few challenges regarding concurrent tests, both technical (scoping services and DbContext) and domain-specific (the same user already having data in the database).

In our fixture class constructor, we create our database as a docker container. To do this, we use the [TestContainer for .NET](https://dotnet.testcontainers.org/) library.

```csharp
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
```

After the container and database is started, we construct a connection string to it. We will have use for it when we create an in-memory process of our API app. 

```csharp
var dbConnectionString = $"Server={DbTestContainer.Hostname},{DbTestContainer.GetMappedPublicPort(DbPort)};" +
    $"Database={DbName};User Id={DbUser};" +
    $"Password={DbPassword};TrustServerCertificate=True";
```

### WebApplicationFactory

Next step in the fixture class constructor, we instantiate a [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1?view=aspnetcore-9.0). 

Because the WebApplicationFactory will create an in-memory instance of our real API server, we can configure the services in it. We don't **have** to do it, actually we'd want to use the already defined configurations of the real API most of the time, but in this case, we need to configure EF Core to connect to our containerized test database.

```csharp
WebApplicationFactory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
    services.AddDbContext<BloqqerDbContext>(options =>
    options.UseSqlServer(dbConnectionString))));
```

The generic type parameter in ```WebApplicationFactory<Program>``` is the entry point class in our API. For this to work, we have added the class declaration at the end our API ```Program.cs``` file. Otherwise we wouldn't be able to reference the Program class in our test project, since we're using top-level statements in the API _Program.cs_ file.

```csharp
// ...

app.Run();

public partial class Program;
```

After configuring the WebApplicationFactory, we apply migrations to the database so that we have all the tables and columns ready.

```csharp
using var dbContext = ScopedServiceProvider.GetRequiredService<BloqqerDbContext>();
dbContext.Database.Migrate();
```

> Note: The ScopedServiceProvider seen above is a member in our fixture class. It returns a scoped service from our APIs DI container. We basically want a new scoped service pretty much everytime we request a service, so that we don't accidentally use a same instance in different threads.
```csharp
public IServiceProvider ScopedServiceProvider =>
    WebApplicationFactory.Services.CreateScope().ServiceProvider;
```

### Authorization

Only thing left to do in our fixture constructor is to create a user and setup authorization for the HttpClient. We will use this HttpClient to call API endpoints in our tests. 
```csharp
var authService = ScopedServiceProvider.GetRequiredService<IAuthService>();
var confirmationCode = authService.RegisterAsync(new(ApiTestUserEmail)).Result;
var registeredUser = authService.ConfirmRegistrationAsync(new(confirmationCode, ApiTestUserName, ApiTestUserPassword)).Result;
var (_, Jwt) = authService.LoginAsync(new(registeredUser.Email, ApiTestUserPassword)).Result;

BloqqerApiClient = WebApplicationFactory.CreateClient();
BloqqerApiClient.DefaultRequestHeaders.Authorization = new("Bearer", Jwt);
```

As seen above, to obtain a JWT, we call the real method implementations of our AuthService. The JWT received is basically a genuine JWT, just like our production app would generate. Attaching that to our HttpClient allows us to call the API endpoints that need authorization.

**Now our assembly fixture setup is ready.**

## The base class

The fixture class is basically a shared singleton. It is instantiated only once. We will also need a base class for all the tests, so we don't have to duplicate code in the test classes.

_IntegrationTestBase.cs_ contains the stuff that pretty much every test might need.

```csharp
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
}
```

The base class is not a singleton, since a new instance is created for each individual test. Therefore, we need to be mindful of tests running in parallel.

The base class receives our IntegrationTestFixture class via constructor injection and makes services available to the tests classes that inherit this base class. It resolves services from the fixture's ScopedServiceProvider, which means whenever a test calls these members, they recieve a service from the DI container but scoped to the calling test only, so they won't affect other parallel tests.

## Testing the API

Here's an example of an API test that checks that the JWT generated by our API contains the correct claims, expiry date etc.

```csharp
[Fact]
public async Task Login_Should_ReturnLoginResponse_WithJwt()
{
    // Arrange
    var username = $"User_{Guid.NewGuid()}";
    var email = $"{Guid.NewGuid()}@bloqqer.net";
    var password = Guid.NewGuid().ToString();

    var user = await UserService.CreateAsync(new UserCreateDto(
        Username: username,
        Email: email),
        TestContext.Current.CancellationToken);

    var loginTime = DateTime.UtcNow.AddMinutes(-1);

    // Act
    var response = await BloqqerApiClient.PostAsJsonAsync("api/auth/login", 
        new LoginRequest(email, password), 
        TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(
        BloqqerJsonSerializerOptions,
        TestContext.Current.CancellationToken);

    Assert.NotNull(loginResponse);
    Assert.NotNull(loginResponse.Jwt);

    var token = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.Jwt);
    Assert.Contains(token.Claims, c => c.Type == "userid" && c.Value == user.UserId.ToString());
    Assert.Contains(token.Claims, c => c.Type == "username" && c.Value == user.Username);
    Assert.Contains(token.Claims, c => c.Type == "email" && c.Value == user.Email);

    var expiresInMinutes = int.Parse(Configuration["Jwt:ExpiresInMinutes"]!);
    var expectedTokenExpiry = loginTime.AddMinutes(expiresInMinutes);
    Assert.True(token.ValidTo >= expectedTokenExpiry);
}
```

The following is an example of a test that checks whether our global exception handling works in the API layer.

## Testing services

The following is an example of a test that checks that all the created articles of a user are returned. Because our test user might have been creating an _n_ amount of articles across all tests, we create a new unique user. This way we can scope the test for that user only, and other tests running in parallel creating articles in the same database don't affect the results.

```csharp
[Fact]
public async Task GetAllByUserIdAsync_Should_ReturnArticles_ForUser()
{
    // Arrange
    var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
        TestContext.Current.CancellationToken);

    var article1 = await ArticleService.CreateAsync(
        new ArticleCreateDto(Content: "Content 1", CreatedById: user.UserId),
        TestContext.Current.CancellationToken);

    var article2 = await ArticleService.CreateAsync(
        new ArticleCreateDto(Content: "Content 2", CreatedById: user.UserId),
        TestContext.Current.CancellationToken);

    // Act
    var results = await ArticleService.GetAllByUserIdAsync(
        userId: user.UserId, 
        includeDeleted: false,
        cancellationToken: TestContext.Current.CancellationToken);

    // Assert
    Assert.NotNull(results);
    Assert.NotEmpty(results);
    Assert.Contains(results, a => a.Id == article1.Id);
    Assert.Contains(results, a => a.Id == article2.Id);
    Assert.Equal(results.Count, DbContext.Articles.Count(a => a.CreatedById == user.UserId));
}
```
## Testing DbContext

The following is an example of a test that checks that our EF Core global query filter works and that soft-deleted entities are not included in the query results.

```csharp
[Fact]
public async Task SoftDeletedEntities_ShouldNot_BeIncludedInQueryResults()
{
    // Arrange
    var entity = DbContext.Articles.Add(new()
    {
        Content = "content",
        CreatedById = TestUser.Id,
        IsDeleted = true,
        DeletedAt = DateTime.UtcNow
    }).Entity;

    DbContext.SaveChanges();

    // Act
    entity = await DbContext.Articles
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.Id == entity.Id,
        TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(entity);
}
```

The following is an example of a test that checks that the EF Core interceptor we have defined in our DbContext works and correctly sets a timestamp whenever an entity is modified.

```csharp
[Fact]
public async Task ModifyEntity_Should_UpdateModifiedAtTimeStamp()
{
    // Arrange & Act
    var entity = DbContext.Articles.Add(new()
    {
        Content = "content",
        CreatedById = TestUser.Id
    }).Entity;

    DbContext.SaveChanges();
    var originalModifiedAt = DbContext.Articles.Find(entity.Id)!.ModifiedAt;

    // Act
    entity.Content = "modified content";
    await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

    // Assert
    var updatedEntity = DbContext.Articles.AsNoTracking().First(a => a.Id == entity.Id);
    Assert.Equal("modified content", updatedEntity.Content);
    Assert.True(originalModifiedAt < updatedEntity.ModifiedAt);
}
```

## 

## Mocking

Sometimes mocking cannot be avoided. A service might need to call a 3rd party API, or a service will need a mocked DateTimeProvider in order to get a static date reliably, instead of relying on system time using DateTime.Now directly.

Mocking is entirely possible in this setup. For mocking, we use the popular [Moq](https://github.com/devlooped/moq) library.

Remember, the HttpClient we configured in the IntegrationTestFixture uses the real service implementations from the Bloqqer.Api DI container. We need a way to replace a service with a mock service, but in a way that it doesn't affect other tests. 

Fortunately, that it made possible by the [ConfigureTestServices](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-9.0#inject-mock-services) method and the WebApplicationFactory. That method will return a HttpClient from the DI container, but overridden services are scoped to the calling test only. In order to make using this functionality available for tests, let's add a helper method to the IntegrationTestBase so that it's available in every test.

```csharp
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
```

This helper creates a new instance of a HttpClient from the WebApplicationFactory, but removes the existing implementation of a service before adding the mock implementation. It also copies the Authorization header from the original HttpClient (configured in the Fixture constructor, remember?) so that the JWT is available in the new HttpClient and we can make http requests to authorized endpoints.

This is an example of a test that requires a mocked service:

```csharp
[Fact]
public async Task Register_Should_SendConfirmationEmail_And_Return_OkResponse()
{
    // Arrange
    var mockEmailService = new Mock<IEmailService>();

    mockEmailService.Setup(service => service.SendRegistrationConfirmationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var client = CreateClientWithMockServices(mockEmailService.Object);

    var email = $"{Guid.NewGuid()}@bloqqer.net";

    // Act
    var response = await client.PostAsJsonAsync("api/auth/register", 
        new RegisterRequest(Email: email),
        TestContext.Current.CancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var responseContents = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    Assert.Equal(responseContents, $"Confirmation email sent to {email}");

    mockEmailService.Verify(service => service.SendRegistrationConfirmationAsync(
        email,
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()
    ), Times.Once);
}
```

This test checks that the SendRegistrationConfirmationAsync method is called during the registration process. Obviously, we don't want to call the real implementation of the SendRegistrationConfirmationAsync because we don't want to be sending any emails anywhere. Therefore, we mock the method, and use the helper we created previously to get a HttpClient where the mock overrides the real service implementation.

```csharp
[Fact]
public async Task Register_Should_SendConfirmationEmail_And_Return_OkResponse()
{
    // ...
	
    var client = CreateClientWithMockServices(mockEmailService.Object);
	
	// ...
}
```

## Final words
You can easily take this project for a spin locally. The only thing required on your machine is Docker, so that IntegrationTestFixture is able to create the database container. That's it.

The test project includes many more tests, many of them modifying the database, in order to showcase that you can run them all in parallel without problems even though a shared database is used.

If you have any suggestions for improvements, please create an issue here in this repo.
