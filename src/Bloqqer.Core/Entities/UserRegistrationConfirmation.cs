namespace Bloqqer.Core.Entities;

public record struct UserRegistrationConfirmationId(Guid Value)
{
    public override readonly string ToString() => Value.ToString();
}

public class UserRegistrationConfirmation : BloqqerEntityBase
{
    public UserRegistrationConfirmationId Id { get; set; } = new(Guid.NewGuid());
    public required string Email { get; set; }
    public required string ConfirmationCode { get; set; }
    public required DateTime ExpiresUtc { get; set; }
}
