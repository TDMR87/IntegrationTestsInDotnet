
namespace Bloqqer.Test.Integration.Api;

public class AuthControllerTests(IntegrationTestFixture Fixture) : IntegrationTestBase(Fixture)
{
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
        var response = await BloqqerApiClient.PostAsJsonAsync("api/auth/login", new LoginRequest(
            Email: email, 
            Password: password), 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Jwt);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.Jwt);
        Assert.Contains(token.Claims, c => c.Type == "userid" && c.Value == user.Id.ToString());
        Assert.Contains(token.Claims, c => c.Type == "username" && c.Value == user.Username);
        Assert.Contains(token.Claims, c => c.Type == "email" && c.Value == user.Email);

        var expiresInMinutes = int.Parse(Configuration["Jwt:ExpiresInMinutes"]!);
        var expectedTokenExpiry = loginTime.AddMinutes(expiresInMinutes);
        Assert.True(token.ValidTo >= expectedTokenExpiry);
    }

    [Fact]
    public async Task Login_Should_ReturnBadRequest_WhenValidationFails()
    {
        // Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/auth/login", new LoginRequest(
            Email: string.Empty, 
            Password: "password"), 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains("Email is required", problemDetails.Detail);
    }

    [Fact]
    public async Task Login_Should_ReturnInternalServerError_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(service => service.LoginAsync(
                It.IsAny<LoginRequestDto>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var client = Fixture.CreateClientWithMockServices(mockAuthService.Object);

        // Act
        var response = await client.PostAsJsonAsync("api/auth/login", 
            new LoginRequest("", ""), 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains("Unexpected error", problemDetails.Detail);
    }
}
