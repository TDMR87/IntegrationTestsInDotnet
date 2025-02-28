namespace Bloqqer.Test.Integration.Services;

public class AuthServiceTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task LoginAsync_Should_ReturnLoginResponseDto()
    {
        // Arrange
        var username = $"User_{Guid.NewGuid()}";
        var email    = $"{Guid.NewGuid()}@bloqqer.net";
        var password = Guid.NewGuid().ToString();

        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: username,
            Email: email),
            TestContext.Current.CancellationToken);

        var loginDto = new LoginRequestDto(email, password);

        // Act
        var result = await AuthService.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Jwt);
        Assert.Equal(user.UserId, result.LoggedInUser.UserId);
        Assert.Equal(user.Username, result.LoggedInUser.Username);
        Assert.Equal(user.Email, result.LoggedInUser.Email);
    }

    [Fact]
    public async Task LoginAsync_Should_ReturnJwt_WithCorrectClaims()
    {
        // Arrange
        var username = $"User_{Guid.NewGuid()}";
        var email = $"{Guid.NewGuid()}@bloqqer.net";
        var password = Guid.NewGuid().ToString();

        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: username,
            Email: email),
            TestContext.Current.CancellationToken);

        var loginDto = new LoginRequestDto(email, password);
        var loginTime = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = await AuthService.LoginAsync(loginDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Jwt);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Jwt);
        Assert.Contains(token.Claims, c => c.Type == "userid" && c.Value == user.UserId.ToString());
        Assert.Contains(token.Claims, c => c.Type == "username" && c.Value == user.Username);
        Assert.Contains(token.Claims, c => c.Type == "email" && c.Value == user.Email);

        var expiresInMinutes = int.Parse(Configuration["Jwt:ExpiresInMinutes"]!);
        var expectedTokenExpiry = loginTime.AddMinutes(expiresInMinutes);
        Assert.True(token.ValidTo >= expectedTokenExpiry);
    }

    [Fact]
    public async Task RegisterAsync_Should_CreateConfirmationCode_InDatabase()
    {
        // Arrange
        var registrationEmail = $"{Guid.NewGuid()}@bloqqer.net";

        // Act
        await AuthService.RegisterAsync(
            new(registrationEmail), 
            TestContext.Current.CancellationToken);

        // Assert. Verify the confirmation was created in the database
        var confirmation = await DbContext.UserRegistrationConfirmations.FirstOrDefaultAsync(
            u => u.Email == registrationEmail, 
            TestContext.Current.CancellationToken);

        Assert.NotNull(confirmation);
        Assert.Equal(registrationEmail, confirmation.Email);
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowValidationException_WhenEmailAlreadyExists()
    {
        // Arrange
        var username = $"User_{Guid.NewGuid()}";
        var email = $"{Guid.NewGuid()}@bloqqer.net";

        await UserService.CreateAsync(new(username, email), TestContext.Current.CancellationToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            AuthService.RegisterAsync(new(email), TestContext.Current.CancellationToken));

        Assert.Contains("Email is already taken", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowValidationException_WhenEmailIsEmpty()
    {
        // Arrange
        var dto = new RegisterRequestDto(Email: string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            AuthService.RegisterAsync(dto, TestContext.Current.CancellationToken));

        Assert.Contains("Email is required", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowValidationException_WhenEmailIsEmpty()
    {
        // Arrange
        var loginDto = new LoginRequestDto(Email: string.Empty, Password: "password");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            AuthService.LoginAsync(loginDto, TestContext.Current.CancellationToken));

        Assert.Contains("Email is required", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowValidationException_WhenPasswordIsEmpty()
    {
        // Arrange
        var loginDto = new LoginRequestDto(
            Email: $"{Guid.NewGuid()}@bloqqer.net", 
            Password: string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            AuthService.LoginAsync(loginDto, TestContext.Current.CancellationToken));

        Assert.Contains("Password is required", exception.Message);
    }
}
