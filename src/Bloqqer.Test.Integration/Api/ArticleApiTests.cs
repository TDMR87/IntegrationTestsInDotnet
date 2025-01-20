using Microsoft.AspNetCore.Mvc;

namespace Bloqqer.Test.Integration.Api;

public class ArticleApiTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task CreateArticle_Should_ReturnCorrectHeaders()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/article",
            new ArticleCreateRequest(Content: "content"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task CreateArticle_Should_Return_SuccessStatusCode()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/article",
            new ArticleCreateRequest(Content: "content"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CreateArticle_Should_Return_ArticleResponse()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/article",
            new ArticleCreateRequest(Content: "content"),
            TestContext.Current.CancellationToken);

        var s = await response.Content.ReadAsStringAsync();

        // Assert
        var article = await response.Content.ReadFromJsonAsync<ArticleResponse>(
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        Assert.IsType<ArticleResponse>(article);
    }

    [Fact]
    public async Task CreateArticle_Should_ReturnArticleResponse_WithCorrectCreatedByUsername()
    {
        // Arrange
        var response = await BloqqerApiClient.PostAsJsonAsync("api/article",
            new ArticleCreateRequest(Content: "content"),
            TestContext.Current.CancellationToken);

        // Act
        var article = await response.Content.ReadFromJsonAsync<ArticleResponse>(
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(article);
        Assert.Equal(TestUser.Id.Value, article.CreatedById);
    }

    [Fact]
    public async Task CreateArticle_Should_ReturnBadRequest_WhenRequestValidationFailed()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.PostAsJsonAsync("api/article",
            new ArticleCreateRequest(Content: string.Empty),
            TestContext.Current.CancellationToken);

        var s = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetArticleById_Should_ReturnArticleResponse_WhenArticleExists()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articleResponse = await BloqqerApiClient.GetFromJsonAsync<ArticleResponse>(
            $"api/article/{article.Id}",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articleResponse);
        Assert.Equal(articleResponse.Id, article.Id.Value);
    }

    [Fact]
    public async Task GetArticleById_Should_ReturnArticle_WithArticleContent()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articleResponse = await BloqqerApiClient.GetFromJsonAsync<ArticleResponse>(
            $"api/article/{article.Id}",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articleResponse);
        Assert.Equal(article.Content, articleResponse.Content);
    }

    [Fact]
    public async Task GetArticleById_Should_ReturnDeletedArticles_WhenIncludeDeletedIsTrue()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        await ArticleService.DeleteAsync(new(
            ArticleId: article.Id,
            DeletedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articleResponse = await BloqqerApiClient.GetFromJsonAsync<ArticleResponse>(
            $"api/article/{article.Id}?includeDeleted=true",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articleResponse);
        Assert.Equal(article.Id.Value, articleResponse.Id);
    }

    [Fact]
    public async Task GetArticleById_Should_ReturnNotFound_WhenArticleDoesNotExist()
    {
        // Arrange & Act
        var articleResponse = await BloqqerApiClient.GetAsync(
            $"api/article/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, articleResponse.StatusCode);
    }

    [Fact]
    public async Task GetAllByUserId_Should_ReturnAllArticlesByUser()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articlesResponse = await BloqqerApiClient.GetFromJsonAsync<List<ArticleResponse>>(
            $"api/article/user/{TestUser.Id}/all",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articlesResponse);
        Assert.NotEmpty(articlesResponse);
        Assert.Equal(articlesResponse.Count, DbContext.Articles.Where(a => a.CreatedById == TestUser.Id).Count());
    }

    [Fact]
    public async Task GetAllByUserId_Should_ReturnEmptyList_WhenUserHasNoArticles()
    {
        // Arrange
        var user = await UserService.CreateAsync(new(
            Username: "newuser", 
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        // Act
        var articlesResponse = await BloqqerApiClient.GetFromJsonAsync<List<ArticleResponse>>(
            $"api/article/user/{user.Id}/all",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articlesResponse);
        Assert.Empty(articlesResponse);
        Assert.Equal(articlesResponse.Count, DbContext.Articles.Where(a => a.CreatedById == user.Id).Count());
    }

    [Fact]
    public async Task GetAllByUserId_Should_ReturnEmptyList_WhenUserDoesNotExist()
    {
        // Arrange & Act
        var articles = await BloqqerApiClient.GetFromJsonAsync<List<ArticleResponse>>(
            $"api/article/user/{Guid.NewGuid()}/all",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articles);
        Assert.Empty(articles);
    }

    [Fact]
    public async Task GetAllByUserId_Should_ReturnDeletedArticles_WhenIncludeDeletedIsTrue()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        await ArticleService.DeleteAsync(new(
            ArticleId: article.Id,
            DeletedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articlesResponse = await BloqqerApiClient.GetFromJsonAsync<List<ArticleResponse>>(
            $"api/article/user/{TestUser.Id}/all?includeDeleted=true",
            DisallowUnmappedMembers,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(articlesResponse);
        Assert.NotEmpty(articlesResponse);
        Assert.Contains(articlesResponse, a => a.Id == article.Id.Value && a.IsDeleted == true);
    }

    [Fact]
    public async Task UpdateArticle_Should_ReturnUpdatedArticle()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articleResponse = await BloqqerApiClient.PutAsJsonAsync($"api/article/{article.Id}", 
            new ArticleUpdateRequest(Content: "Updated content"),
            TestContext.Current.CancellationToken);

        // Assert
        var updatedArticle = await articleResponse.Content.ReadFromJsonAsync<ArticleResponse>(
            DisallowUnmappedMembers, TestContext.Current.CancellationToken);

        Assert.NotNull(updatedArticle);
        Assert.Equal("Updated content", updatedArticle.Content);
    }

    [Fact]
    public async Task UpdateArticle_Should_ReturnNotFound_WhenArticleDoesNotExist()
    {
        // Arrange & Act
        var articleResponse = await BloqqerApiClient.PutAsJsonAsync($"api/article/{Guid.NewGuid()}",
            new ArticleUpdateRequest(Content: "Updated content"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, articleResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_Should_ReturnUnauthorized_WhenUpdatedByIsNotArticleCreator()
    {
        // Arrange
        var user = await UserService.CreateAsync(new(
            Username: "newuser",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var article = await ArticleService.CreateAsync(new(
             Content: "content",
             CreatedById: user.Id),
             TestContext.Current.CancellationToken);

        // Act (updating the article as the integration test user)
        var articleResponse = await BloqqerApiClient.PutAsJsonAsync($"api/article/{article.Id}",
            new ArticleUpdateRequest(Content: "Updated content"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, articleResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_Should_ReturnBadRequest_WhenRequestValidationFailed()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var articleResponse = await BloqqerApiClient.PutAsJsonAsync($"api/article/{article.Id}",
            new ArticleUpdateRequest(Content: string.Empty),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, articleResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_Should_ReturnSuccessStatusCode()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new(
            Content: "content",
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var response = await BloqqerApiClient.DeleteAsync($"api/article/{article.Id.Value}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DeleteArticle_Should_ReturnNotFound_WhenArticleDoesNotExist()
    {
        // Arrange & Act
        var response = await BloqqerApiClient.DeleteAsync($"api/article/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
