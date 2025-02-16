namespace Bloqqer.Services.Validators;

public  class ArticleCreateValidator : AbstractValidator<ArticleCreateDto>
{
    public ArticleCreateValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required");
    }
}
