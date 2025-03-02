namespace Bloqqer.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<string> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> ConfirmRegistrationAsync(RegistrationConfirmationRequestDto dto, CancellationToken cancellationToken = default);
}

public class AuthService(
    BloqqerDbContext dbContext, 
    IUserRegistrationConfirmationService confirmationService,
    IValidator<LoginRequestDto> loginRequestValidator, 
    IValidator<RegisterRequestDto> registerRequestValidator,
    IConfiguration configuration) : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await loginRequestValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BloqqerValidationException(string.Join("\n", validationResult.Errors));
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
        if (user is null)
        {
            throw new BloqqerUnauthorizedException();
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var jwt = GenerateJwt(user);

        return new LoginResponseDto(new(
            UserId: user.Id, 
            Username: user.Username, 
            Email: user.Email),
            Jwt: jwt
        );
    }

    public async Task<string> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validationResult = await registerRequestValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BloqqerValidationException(string.Join("\n", validationResult.Errors));
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
        if (user is not null)
        {
            throw new BloqqerValidationException("Email is already taken");
        }

        var confirmationCode = Guid.NewGuid().ToString();

        await confirmationService.CreateAsync(new(
            Email: dto.Email,
            ConfirmationCode: confirmationCode,
            ExpiresUtc: DateTime.UtcNow.AddDays(1)), 
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return confirmationCode;
    }

    public async Task<RegisterResponseDto> ConfirmRegistrationAsync(RegistrationConfirmationRequestDto dto, CancellationToken cancellationToken = default)
    {
        var confirmation = await confirmationService.GetByConfirmationCodeAsync(
            dto.ConfirmationCode, cancellationToken);

        if (confirmation is null)
        {
            throw new BloqqerValidationException("Invalid confirmation code");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == confirmation.Email, cancellationToken);
        if (user is not null)
        {
            throw new BloqqerValidationException("Email is already taken");
        }

        user = new() { Email = confirmation.Email, Username = dto.Username };
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await confirmationService.DeleteAsync(confirmation.ConfirmationCode, cancellationToken);

        return new RegisterResponseDto(user.Id, user.Username, user.Email);
    }

    private string GenerateJwt(User user)
    {
        var claims = new[]
        {
            new Claim("userid", user.Id.Value.ToString()),
            new Claim("username", user.Username),
            new Claim("email", user.Email),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(int.Parse(configuration["Jwt:ExpiresInMinutes"]!)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
