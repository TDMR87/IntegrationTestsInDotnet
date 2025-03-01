namespace Bloqqer.Database.EntityConfiguration;

public class UserRegistrationConfirmationConfiguration : IEntityTypeConfiguration<UserRegistrationConfirmation>
{
    public void Configure(EntityTypeBuilder<UserRegistrationConfirmation> builder)
    {
        builder
            .ToTable("UserRegistrationConfirmation")
            .HasKey(c => c.Id);

        builder
            .Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new UserRegistrationConfirmationId(value)
            ).ValueGeneratedOnAdd();
    }
}
