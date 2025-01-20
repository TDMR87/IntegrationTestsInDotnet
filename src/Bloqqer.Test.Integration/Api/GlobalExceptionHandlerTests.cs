namespace Bloqqer.Test.Integration.Api;

public class GlobalExceptionHandlerTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    

    //[Fact]
    //public async Task UnsuccessfulApiRequests_Should_ReturnProblemDetails()
    //{
    //    // Arrange
    //    // Act
    //    var response = await ApiClient.GetAsync($"api/thread/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

    //    // Assert
    //    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
    //    Assert.NotNull(problemDetails);
    //}
}
