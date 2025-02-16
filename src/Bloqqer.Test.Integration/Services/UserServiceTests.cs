namespace Bloqqer.Test.Integration.Services;

public class UserServiceTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task CreateAsync_Should_CreateUser_And_ReturnUserDto()
    {
        // Arrange
        var dto = new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net");

        // Act
        var user = await UserService.CreateAsync(dto ,TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(user.Username, dto.Username);
        Assert.Equal(dto.Email, dto.Email);
        var userInDb = await DbContext.Users.FindAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(userInDb);
        Assert.Equal(dto.Username, userInDb.Username);
        Assert.Equal(dto.Email, userInDb.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_Should_ReturnUserDto_WhenUserExists()
    {
        // Arrange
        var email = $"{Guid.NewGuid()}@bloqqer.net";

        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}", 
            Email: email),
            TestContext.Current.CancellationToken);

        // Act
        var result = await UserService.GetByEmailAsync(email, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_Should_ThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentEmail = $"{Guid.NewGuid()}@bloqqer.net";

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerNotFoundException>(() =>
            UserService.GetByEmailAsync(nonExistentEmail, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnUserDto_WhenUserExists()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}", 
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        // Act
        var result = await UserService.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = new UserId(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerNotFoundException>(() =>
            UserService.GetByIdAsync(nonExistentUserId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateUser_And_ReturnUpdatedUserDto()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var updateDto = new UserUpdateDto(
            UserId: user.Id,
            Username: "UpdatedUsername",
            UpdatedById: user.Id);

        // Act
        var result = await UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Username, result.Username);
        var updatedUserInDb = await DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUserInDb);
        Assert.Equal(updateDto.Username, updatedUserInDb.Username);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowValidationException_WhenUsernameIsEmpty()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var updateDto = new UserUpdateDto(
            UserId: user.Id,
            Username: string.Empty,
            UpdatedById: user.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));

        Assert.Contains("Username is required", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowValidationException_WhenUsernameIsTooShort()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var updateDto = new UserUpdateDto(
            UserId: user.Id,
            Username: "abc",
            UpdatedById: user.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));

        Assert.Contains("Username must be between 5 and 50 characters", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowValidationException_WhenUsernameIsTooLong()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var updateDto = new UserUpdateDto(
            UserId: user.Id,
            Username: $"User_{Guid.NewGuid()}{Guid.NewGuid()}",
            UpdatedById: user.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));

        Assert.Contains("Username must be between 5 and 50 characters", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowUnauthorizedException_WhenUpdatedByOtherUser()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var otherUser = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var updateDto = new UserUpdateDto(
            UserId: user.Id,
            Username: $"User_{Guid.NewGuid()}",
            UpdatedById: otherUser.Id);

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerUnauthorizedException>(() =>
            UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = new UserId(Guid.NewGuid());

        var updateDto = new UserUpdateDto(
            UserId: nonExistentUserId,
            Username: $"User_{Guid.NewGuid()}",
            UpdatedById: nonExistentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerNotFoundException>(() =>
            UserService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));
    }
}
