namespace Bloqqer.Services.Validators;

public class ArticleDeleteValidator : AbstractValidator<ArticleDeleteDto>
{
    public ArticleDeleteValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty()
            .WithMessage("ArticleId is required");

        RuleFor(x => x.DeletedById)
            .NotEmpty()
            .WithMessage("DeletedById is required");
    }
}
