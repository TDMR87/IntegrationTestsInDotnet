namespace Bloqqer.Services;

public interface IUserRegistrationConfirmationService
{
    Task<UserRegistrationConfirmationDto> CreateAsync(
        UserRegistrationConfirmationCreateDto dto, 
        CancellationToken cancellationToken = default);

    Task<UserRegistrationConfirmationDto> GetByConfirmationCodeAsync(
        string confirmationCode, 
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string confirmationCode,
        CancellationToken cancellationToken = default);
}

public class UserRegistrationConfirmationService(BloqqerDbContext dbContext) : IUserRegistrationConfirmationService
{
    public async Task<UserRegistrationConfirmationDto> CreateAsync(
        UserRegistrationConfirmationCreateDto dto, 
        CancellationToken cancellationToken = default)
    {
        var entity = (await dbContext.UserRegistrationConfirmations.AddAsync(new()
        {
            Email = dto.Email,
            ConfirmationCode = dto.ConfirmationCode,
            ExpiresUtc = dto.ExpiresUtc
        }, cancellationToken)).Entity;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new(
            Id: entity.Id,
            Email: entity.Email,
            ConfirmationCode: entity.ConfirmationCode,
            ExpiresUtc: entity.ExpiresUtc);
    }

    public async Task<UserRegistrationConfirmationDto> GetByConfirmationCodeAsync(
        string confirmationCode, 
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserRegistrationConfirmations
            .Where(urc => urc.ConfirmationCode == confirmationCode)
            .Select(urc => new UserRegistrationConfirmationDto(
                urc.Id,
                urc.Email,
                urc.ConfirmationCode,
                urc.ExpiresUtc))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BloqqerNotFoundException("Confirmation code not found");
    }

    public async Task DeleteAsync(string confirmationCode, CancellationToken cancellationToken = default)
    {
        var entity = dbContext.UserRegistrationConfirmations
            .FirstOrDefault(urc => urc.ConfirmationCode == confirmationCode)
            ?? throw new BloqqerNotFoundException("Confirmation code not found");

        dbContext.UserRegistrationConfirmations.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
