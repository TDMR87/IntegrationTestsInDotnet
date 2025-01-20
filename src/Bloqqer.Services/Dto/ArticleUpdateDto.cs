namespace Bloqqer.Services.Dto;

public record ArticleUpdateDto(
    ArticleId ArticleId,
    UserId UpdatedById,
    string Content
);