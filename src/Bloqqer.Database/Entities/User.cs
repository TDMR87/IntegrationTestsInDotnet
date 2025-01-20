namespace Bloqqer.Database.Entities;

public record struct UserId(Guid Value)
{
    public override readonly string ToString() => Value.ToString();
}

public class User : BloqqerEntityBase
{
    public UserId Id { get; set; } = new(Guid.NewGuid());
    public List<Article> Articles { get; set; } = [];
    public required string Username { get; set; }
    public required string Email { get; set; }
    public DateTime LastLoginAt { get; set; }

    [NotMapped]
    public const int MaxUsernameLength = 100;
}
