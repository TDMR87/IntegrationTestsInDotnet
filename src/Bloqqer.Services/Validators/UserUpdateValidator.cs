namespace Bloqqer.Services.Validators;

public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required");

        RuleFor(x => x.Username)
            .Length(5, 50)
            .WithMessage("Username must be between 5 and 50 characters");
    }
}