namespace Bloqqer.Database.EntityConfiguration;

internal class ArticleEntityConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder
            .ToTable("Article")
            .HasKey(article => article.Id);

        builder
            .Property(article => article.Id)
            .HasConversion(
                id => id.Value,
                value => new ArticleId(value)
            ).ValueGeneratedOnAdd();

        builder
            .Property(article => article.Content)
            .HasMaxLength(Article.MaxContentLength);

        builder
            .HasOne(article => article.CreatedBy)
            .WithMany(user => user.Articles)
            .HasForeignKey(article => article.CreatedById)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
