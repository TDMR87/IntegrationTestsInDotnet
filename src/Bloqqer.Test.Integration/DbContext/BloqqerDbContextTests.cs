namespace Bloqqer.Test.Integration.DbContext;

public class BloqqerDbContextTests(IntegrationTestFixture _) : IntegrationTestBase(_)
{
    [Fact]
    public async Task SoftDeletedEntity_Should_BeIncludedInQueryResults_WhenIgnoreQueryFiltersIsUsed()
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
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == entity.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entity);
        Assert.True(entity.IsDeleted);
    }

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

    [Fact]
    public async Task RemoveEntity_Should_SoftDeleteEntity()
    {
        // Arrange
        var entity = DbContext.Articles.Add(new()
        {
            Content = "content",
            CreatedById = TestUser.Id,
        }).Entity;

        DbContext.SaveChanges();

        // Act
        DbContext.Articles.Remove(entity);
        DbContext.SaveChanges();

        // Assert
        entity = await DbContext.Articles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == entity.Id,
            TestContext.Current.CancellationToken);

        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public async Task RemoveEntity_Should_SetDeletedAtTimestamp()
    {
        // Arrange
        var entity = DbContext.Articles.Add(new()
        {
            Content = "content",
            CreatedById = TestUser.Id,
        }).Entity;

        DbContext.SaveChanges();

        // Act
        DbContext.Remove(entity);
        DbContext.SaveChanges();

        // Assert
        entity = await DbContext.Articles
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == entity.Id,
            TestContext.Current.CancellationToken);

        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
    }

    [Fact]
    public async Task AddEntity_ShouldNot_SetDeletedFlagAndTimestamp()
    {
        // Arrange & Act
        var entity = DbContext.Articles.Add(new()
        {
            Content = "content",
            CreatedById = TestUser.Id
        }).Entity;

        DbContext.SaveChanges();

        // Assert
        entity = await DbContext.Articles
            .AsNoTracking()
            .FirstAsync(a => a.Id == entity.Id,
            TestContext.Current.CancellationToken);

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
    }

    [Fact]
    public async Task AddEntity_Should_SetCreatedAtTimestamp()
    {
        // Arrange & Act
        var entity = DbContext.Articles.Add(new()
        {
            Content = "content",
            CreatedById = TestUser.Id
        }).Entity;

        DbContext.SaveChanges();

        // Assert
        entity = await DbContext.Articles
            .AsNoTracking()
            .FirstAsync(a => a.Id == entity.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(entity?.CreatedAt);
    }

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
}
