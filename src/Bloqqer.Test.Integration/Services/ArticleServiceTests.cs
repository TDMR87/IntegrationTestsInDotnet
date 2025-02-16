namespace Bloqqer.Test.Integration.Services;

public class ArticleServiceTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task CreateAsync_Should_CreateArticle_And_ReturnArticleDto()
    {
        // Arrange
        var dto = new ArticleCreateDto(Content: "Test Content", CreatedById: TestUser.Id);

        // Act
        var result = await ArticleService.CreateAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Content, result.Content);
        Assert.Equal(dto.CreatedById, result.CreatedById);
        Assert.False(result.IsDeleted);

        var articleInDb = await DbContext.Articles.FindAsync(result.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(articleInDb);
        Assert.Equal(dto.Content, articleInDb.Content);
        Assert.Equal(dto.CreatedById, articleInDb.CreatedById);
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowValidationException_WhenContentIsEmpty()
    {
        // Arrange
        var dto = new ArticleCreateDto(Content: string.Empty, CreatedById: TestUser.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BloqqerValidationException>(() =>
            ArticleService.CreateAsync(dto, TestContext.Current.CancellationToken));

        Assert.Contains("Content", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnArticleDto_WhenArticleExists()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(
            new ArticleCreateDto(Content: "Test Content", CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        // Act
        var result = await ArticleService.GetByIdAsync(article.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(article.Id, result.Id);
        Assert.Equal(article.Content, result.Content);
        Assert.Equal(article.CreatedById, result.CreatedById);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ThrowNotFoundException_WhenArticleDoesNotExist()
    {
        // Arrange
        var nonExistentArticleId = new ArticleId(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerNotFoundException>(() =>
            ArticleService.GetByIdAsync(
                articleId: nonExistentArticleId,
                includeDeleted: false, 
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAllByUserIdAsync_Should_ReturnArticles_ForUser()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
                Username: $"User_{Guid.NewGuid()}",
                Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var article1 = await ArticleService.CreateAsync(
            new ArticleCreateDto(Content: "Content 1", CreatedById: user.Id),
            TestContext.Current.CancellationToken);

        var article2 = await ArticleService.CreateAsync(
            new ArticleCreateDto(Content: "Content 2", CreatedById: user.Id),
            TestContext.Current.CancellationToken);

        // Act
        var result = await ArticleService.GetAllByUserIdAsync(
            userId: user.Id, 
            includeDeleted: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, a => a.Id == article1.Id);
        Assert.Contains(result, a => a.Id == article2.Id);
        Assert.Equal(result.Count, DbContext.Articles.Count(a => a.CreatedById == user.Id));
    }

    [Fact]
    public async Task GetAllByUserIdAsync_Should_ReturnEmptyList_WhenUserHasNoArticles()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
                Username: $"User_{Guid.NewGuid()}",
                Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        // Act
        var result = await ArticleService.GetAllByUserIdAsync(
            userId: user.Id,
            includeDeleted: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateArticle_And_ReturnUpdatedArticleDto()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new ArticleCreateDto(
            Content: "Original Content", 
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        var updateDto = new ArticleUpdateDto(
            ArticleId: article.Id, 
            Content: "Updated Content", 
            UpdatedById: TestUser.Id);

        // Act
        var result = await ArticleService.UpdateAsync(updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Content, result.Content);

        var updatedArticleInDb = await DbContext.Articles.FindAsync(article.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updatedArticleInDb);
        Assert.Equal(updateDto.Content, updatedArticleInDb.Content);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowUnauthorizedException_WhenUpdatedByIsNotCreator()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var article = await ArticleService.CreateAsync(new ArticleCreateDto(
            Content: "Original Content", 
            CreatedById: user.Id),
            TestContext.Current.CancellationToken);

        var updateDto = new ArticleUpdateDto(
            ArticleId: article.Id, 
            Content: "Updated Content", 
            UpdatedById: TestUser.Id);

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerUnauthorizedException>(() =>
            ArticleService.UpdateAsync(updateDto, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDeleteArticle()
    {
        // Arrange
        var article = await ArticleService.CreateAsync(new ArticleCreateDto(
            Content: "Test Content", 
            CreatedById: TestUser.Id),
            TestContext.Current.CancellationToken);

        var deleteDto = new ArticleDeleteDto(
            ArticleId: article.Id, 
            DeletedById: TestUser.Id);

        // Act
        await ArticleService.DeleteAsync(deleteDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, await DbContext.Articles
            .IgnoreQueryFilters()
            .CountAsync(a => a.Id == article.Id && a.IsDeleted, 
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowNotFoundException_WhenArticleDoesNotExist()
    {
        // Arrange
        var nonExistentArticleId = new ArticleId(Guid.NewGuid());

        var deleteDto = new ArticleDeleteDto(
            ArticleId: nonExistentArticleId, 
            DeletedById: TestUser.Id);

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerNotFoundException>(() =>
            ArticleService.DeleteAsync(deleteDto, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowUnauthorizedException_WhenDeletedByIsNotArticleCreator()
    {
        // Arrange
        var user = await UserService.CreateAsync(new UserCreateDto(
            Username: $"User_{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@bloqqer.net"),
            TestContext.Current.CancellationToken);

        var article = await ArticleService.CreateAsync(new ArticleCreateDto(
            Content: "Test Content",
            CreatedById: user.Id),
            TestContext.Current.CancellationToken);

        var deleteDto = new ArticleDeleteDto(
            ArticleId: article.Id,
            DeletedById: TestUser.Id); // Not the creator

        // Act & Assert
        await Assert.ThrowsAsync<BloqqerUnauthorizedException>(() =>
            ArticleService.DeleteAsync(deleteDto, TestContext.Current.CancellationToken));
    }
}
