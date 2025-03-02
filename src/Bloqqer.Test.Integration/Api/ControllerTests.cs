namespace Bloqqer.Test.Integration.Api;

public class ControllerTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task InvalidRoutePath_Should_ReturnNotFound()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.GetAsync("api/invalid-path",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledExceptions_Should_Return_InternalServerError_And_ProblemDetails()
    {
        // Arrange
        var mockArticleService = new Mock<IArticleService>();

        mockArticleService
            .Setup(service => service
            .GetByIdAsync(
                It.IsAny<ArticleId>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
            .Throws(new Exception("Simulated exception"));

        var client = CreateClientWithMockServices(mockArticleService.Object);

        // Act
        var response = await client.GetAsync($"api/article/{Guid.NewGuid()}", 
            TestContext.Current.CancellationToken);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            BloqqerJsonSerializerOptions,
            TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.IsType<ProblemDetails>(problemDetails);
        Assert.Equal(problemDetails.Status, (int)HttpStatusCode.InternalServerError);
    }
}
