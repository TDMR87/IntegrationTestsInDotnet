namespace Bloqqer.Services.Dto;

public record ArticleDto(
    ArticleId Id,
    string Content, 
    UserId CreatedById,
    bool IsDeleted
);
