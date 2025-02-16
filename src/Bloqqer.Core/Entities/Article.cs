namespace Bloqqer.Core.Entities;

public record struct ArticleId(Guid Value)
{
    public override readonly string ToString() => Value.ToString();
}

public class Article : BloqqerEntityBase
{
    // Database columns
    public ArticleId Id { get; set; } = new(Guid.NewGuid());
    public required UserId CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public required string Content { get; set; }

    // Not mapped to database
    public const int MaxContentLength = 1000;
}
