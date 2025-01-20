namespace Bloqqer.Api.Models;

public record ArticleResponse(
    Guid Id, 
    string Content, 
    Guid CreatedById,
    bool IsDeleted);