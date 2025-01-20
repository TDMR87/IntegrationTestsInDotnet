namespace Bloqqer.Database.Entities;

public record struct ArticleId(Guid Value)
{
    public override readonly string ToString() => Value.ToString();
}

public class Article : BloqqerEntityBase
{
    public ArticleId Id { get; set; } = new(Guid.NewGuid());
    public required UserId CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public required string Content { get; set; }

    [NotMapped]
    public const int MaxContentLength = 1000;
}
