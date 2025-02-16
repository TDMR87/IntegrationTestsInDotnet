namespace Bloqqer.Services.Validators;

public class ArticleUpdateValidator : AbstractValidator<ArticleUpdateDto>
{
    public ArticleUpdateValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required");
    }
}