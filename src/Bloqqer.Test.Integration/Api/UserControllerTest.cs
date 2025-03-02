using Bloqqer.Api.Dto;
using System.Net.Http.Headers;

namespace Bloqqer.Test.Integration.Api;

public class UserControllerTest(IntegrationTestFixture Fixture) : IntegrationTestBase(Fixture)
{
    [Fact]
    public async Task CreateUser_Should_ReturnOk_WithUserResponse()
    {
        // Arrange
        var dto = new UserCreateDto(Username: "TestUser", Email: "test@bloqqer.net");

        // Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/user", 
            dto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(dto.Username, user.Username);
        Assert.Equal(dto.Email, user.Email);
    }

    [Fact]
    public async Task GetUserById_Should_ReturnOk_WithUserResponse()
    {
        // Arrange
        var user = await UserService.CreateAsync(
            new UserCreateDto(Username: "TestUser", Email: "test@bloqqer.net"),
            TestContext.Current.CancellationToken);

        // Act
        var response = await BloqqerApiClient.GetAsync($"api/user/id/{user.UserId}", 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(userResponse);
        Assert.Equal(user.UserId.Value, userResponse.Id);
        Assert.Equal(user.Username, userResponse.Username);
        Assert.Equal(user.Email, userResponse.Email);
    }

    [Fact]
    public async Task GetUserById_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = new UserId(Guid.NewGuid());

        // Act
        var response = await BloqqerApiClient.GetAsync($"api/user/id/{nonExistentUserId}", 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains($"User with ID {nonExistentUserId} not found", problemDetails.Detail);
    }

    [Fact]
    public async Task GetUserByEmail_Should_ReturnOk_WithUserResponse()
    {
        // Arrange
        var email = $"{Guid.NewGuid()}@bloqqer.net";
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}", 
            Email: email),
            TestContext.Current.CancellationToken);

        // Act
        var response = await BloqqerApiClient.GetAsync($"api/user/email/{email}", 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(userResponse);
        Assert.Equal(user.UserId.Value, userResponse.Id);
        Assert.Equal(user.Username, userResponse.Username);
        Assert.Equal(user.Email, userResponse.Email);
    }

    [Fact]
    public async Task GetUserByEmail_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentEmail = $"{Guid.NewGuid()}@bloqqer.net";

        // Act
        var response = await BloqqerApiClient.GetAsync($"api/user/email/{nonExistentEmail}", 
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains($"User with email {nonExistentEmail} not found", problemDetails.Detail);
    }

    [Fact]
    public async Task UpdateUser_Should_ReturnOk_WithUpdatedUserResponse()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}", 
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var jwt = await AuthService.LoginAsync(new LoginRequestDto(
            Email: user.Email, 
            Password: "somepass"),
            TestContext.Current.CancellationToken);

        var client = Fixture.WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt.Jwt);

        var updateRequest = new UserUpdateRequest(Username: "UpdatedUsername");

        // Act
        var response = await client.PutAsJsonAsync($"api/user/{user.UserId}", 
            updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedUser = await response.Content.ReadFromJsonAsync<UserResponse>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(updatedUser);
        Assert.Equal(updateRequest.Username, updatedUser.Username);
    }

    [Fact]
    public async Task UpdateUser_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var updateRequest = new UserUpdateRequest(Username: "UpdatedUsername");

        // Act
        var response = await BloqqerApiClient.PutAsJsonAsync($"api/user/{nonExistentUserId}", 
            updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains($"User with ID {nonExistentUserId} not found", problemDetails.Detail);
    }

    [Fact]
    public async Task UpdateUser_Should_ReturnUnauthorized_WhenUpdatedByOtherUser()
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

        var updateRequest = new UserUpdateRequest(Username: "UpdatedUsername");

        // Act (updating the user as another user)
        var response = await BloqqerApiClient.PutAsJsonAsync($"api/user/{user.UserId}", 
            updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Contains("Users are only allowed to update their own profile", problemDetails.Detail);
    }
}
