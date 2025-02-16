namespace Bloqqer.Api.Dto;

public record ArticleResponse(
    Guid Id, 
    string Content, 
    Guid CreatedById,
    bool IsDeleted);