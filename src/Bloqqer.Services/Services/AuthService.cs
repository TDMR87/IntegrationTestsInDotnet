using Bloqqer.Core.Exceptions;
using FluentValidation;

namespace Bloqqer.Services.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}

public class AuthService(
    BloqqerDbContext dbContext, 
    IValidator<LoginRequestDto> loginRequestValidator, 
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
            user = new() { Email = dto.Email, Username = dto.Email, LastLoginAt = DateTime.UtcNow };
            await dbContext.Users.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var jwt = GenerateJwt(user);

        return new LoginResponseDto(new(
            Id: user.Id, 
            Username: user.Username, 
            Email: user.Email),
            Jwt: jwt
        );
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
