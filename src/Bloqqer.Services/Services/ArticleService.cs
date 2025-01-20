using Bloqqer.Core.Exceptions;
using FluentValidation;

namespace Bloqqer.Services.Services;

public interface IArticleService
{
    Task<List<ArticleDto>> GetAllByUserIdAsync(UserId userId, bool? includeDeleted = false, CancellationToken cancellationToken = default);
    Task<ArticleDto> GetByIdAsync(ArticleId articleId, bool? includeDeleted = false, CancellationToken cancellationToken = default);
    Task<ArticleDto> CreateAsync(ArticleCreateDto dto, CancellationToken cancellationToken = default);
    Task<ArticleDto> UpdateAsync(ArticleUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(ArticleDeleteDto dto, CancellationToken cancellationToken = default);
}

public class ArticleService(
    BloqqerDbContext dbContext, 
    IValidator<ArticleCreateDto> createValidator,
    IValidator<ArticleUpdateDto> updateValidator) : IArticleService
{
    public async Task<ArticleDto> CreateAsync(ArticleCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BloqqerValidationException(string.Join("\n", validationResult.Errors));
        }

        var entity = dbContext.Add(new Article
        {
            Content = dto.Content,
            CreatedById = dto.CreatedById
        }).Entity;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArticleDto(
            entity.Id, 
            entity.Content, 
            entity.CreatedById, 
            entity.IsDeleted
        );
    }

    public async Task DeleteAsync(ArticleDeleteDto dto, CancellationToken cancellationToken = default)
    {
        var article = await dbContext.Articles.FindAsync(dto.ArticleId, cancellationToken) 
            ?? throw new BloqqerNotFoundException($"Article with id {dto.ArticleId} not found");

        if (article.CreatedById != dto.DeletedById)
        {
            throw new BloqqerUnauthorizedException("Only the creator is allowed to delete an article");
        }

        dbContext.Articles.Remove(article);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<ArticleDto>> GetAllByUserIdAsync(UserId userId, bool? includeDeleted = false, CancellationToken cancellationToken = default)
    {
        return dbContext.Articles
            .Where(x => x.CreatedById == userId)
            .If(includeDeleted == true, x => x.IgnoreQueryFilters())
            .Select(x => new ArticleDto(
                x.Id,
                x.Content,
                x.CreatedById,
                x.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<ArticleDto> GetByIdAsync(ArticleId articleId, bool? includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var dto = await dbContext.Articles
            .Where(x => x.Id == articleId)
            .If(includeDeleted == true, x => x.IgnoreQueryFilters())
            .Select(x => new ArticleDto(
                x.Id, 
                x.Content, 
                x.CreatedById, 
                x.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is not null 
            ? dto
            : throw new BloqqerNotFoundException($"Article with id {articleId} not found");
    }

    public async Task<ArticleDto> UpdateAsync(ArticleUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var updatedBy = dbContext.Users.Find(dto.UpdatedById)
            ?? throw new BloqqerUnauthorizedException($"User with id {dto.UpdatedById} not found");

        var entity = dbContext.Articles.Find(dto.ArticleId)
            ?? throw new BloqqerNotFoundException($"Article with id {dto.ArticleId} not found");

        if (updatedBy.Id != entity.CreatedById)
        {
            throw new BloqqerUnauthorizedException("Only the creator is allowed to update an article");
        }

        var validationResult = await updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BloqqerValidationException(string.Join("\n", validationResult.Errors));
        }

        entity.Content = dto.Content;
        dbContext.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(
            entity.Id,
            entity.Content,
            entity.CreatedById,
            entity.IsDeleted
        );
    }
}
