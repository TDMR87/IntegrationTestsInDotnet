namespace Bloqqer.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArticleController(IArticleService articleService) : BloqqerControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] bool? includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var article = await articleService.GetByIdAsync(
            articleId: new ArticleId(id),
            includeDeleted: includeDeleted,
            cancellationToken);

        return Ok(new ArticleResponse(
            Id: article.Id.Value,
            Content: article.Content,
            CreatedById: article.CreatedById.Value,
            IsDeleted: article.IsDeleted));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await articleService.DeleteAsync(new(
            ArticleId: new ArticleId(id),
            DeletedById: new UserId(CurrentUserId)), 
            cancellationToken);

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ArticleCreateRequest request, CancellationToken cancellationToken = default)
    {
        var article = await articleService.CreateAsync(new(
            Content: request.Content,
            CreatedById: new UserId(CurrentUserId)), 
            cancellationToken);

        return Ok(new ArticleResponse(
            Id: article.Id.Value,
            Content: article.Content,
            CreatedById: article.CreatedById.Value,
            IsDeleted: article.IsDeleted));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ArticleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var article = await articleService.UpdateAsync(new(
            ArticleId: new ArticleId(id),
            UpdatedById: new UserId(CurrentUserId),
            Content: request.Content), 
            cancellationToken);

        return Ok(new ArticleResponse(
            Id: article.Id.Value,
            Content: article.Content,
            CreatedById: article.CreatedById.Value,
            IsDeleted: article.IsDeleted));
    }

    [HttpGet("user/{userId}/all")]
    public async Task<IActionResult> GetAllByUserId(Guid userId, [FromQuery] bool? includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var articles = await articleService.GetAllByUserIdAsync(
            userId: new UserId(userId),
            includeDeleted: includeDeleted,
            cancellationToken);

        return Ok(articles.Select(article => new ArticleResponse(
            Id: article.Id.Value,
            Content: article.Content,
            CreatedById: article.CreatedById.Value,
            IsDeleted: article.IsDeleted))
            .ToList());
    }

}