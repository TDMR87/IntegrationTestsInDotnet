namespace Bloqqer.Services;

public interface IUserService
{
    public Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
    public Task<UserDto> UpdateAsync(UserUpdateDto dto, CancellationToken cancellationToken = default);
    public Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken);
    public Task<UserDto> GetByIdAsync(UserId id, CancellationToken cancellationToken);
}

public class UserService(
    BloqqerDbContext dbContext,
    IValidator<UserUpdateDto> userUpdateValidator) : IUserService
{
    public async Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = dbContext.Users.Add(new()
        {
            Username = dto.Username,
            Email = dto.Email
        }).Entity;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new(
            UserId: entity.Id,
            Username: entity.Username,
            Email: entity.Email);
    }

    public async Task<UserDto> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken) 
            ?? throw new BloqqerNotFoundException($"User with email {email} not found");

        return new(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email);
    }

    public async Task<UserDto> GetByIdAsync(UserId id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken) 
            ?? throw new BloqqerNotFoundException($"User with ID {id} not found");

        return new(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email);
    }

    public async Task<UserDto> UpdateAsync(UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await userUpdateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BloqqerValidationException(string.Join("\n", validationResult.Errors));
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, cancellationToken) 
            ?? throw new BloqqerNotFoundException($"User with ID {dto.UserId} not found");

        if (dto.UpdatedById != user.Id)
        {
            throw new BloqqerUnauthorizedException("Users are only allowed to update their own profile");
        }

        user.Username = dto.Username;
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email);
    }
}
