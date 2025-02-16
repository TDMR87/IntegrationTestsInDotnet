namespace Bloqqer.Services.Validators;

public class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator()
    {
        RuleFor(x => x.Username)
            .Length(5, 50)
            .WithMessage("Username must be between 5 and 50 characters");

        RuleFor(x => x.Email)
            .EmailAddress();
    }
}
