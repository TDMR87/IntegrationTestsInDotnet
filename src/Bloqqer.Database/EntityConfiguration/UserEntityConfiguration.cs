namespace Bloqqer.Database.EntityConfiguration;

internal class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .ToTable("Users")
            .HasKey(user => user.Id);

        builder
            .Property(user => user.Id)
            .HasConversion(
                id => id.Value,
                value => new UserId(value)
            ).ValueGeneratedOnAdd();

        builder
            .Property(user => user.Username)
            .HasMaxLength(User.MaxUsernameLength);

        builder
            .HasMany(user => user.Articles)
            .WithOne(article => article.CreatedBy)
            .HasForeignKey(article => article.CreatedById)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}