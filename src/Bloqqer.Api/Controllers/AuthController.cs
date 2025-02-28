
namespace Bloqqer.Api.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IAuthService authService, IEmailService emailService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken = default)
    {
        var dto = await authService.LoginAsync(new(
            Email: request.Email,
            Password: request.Password),
            cancellationToken
        );

        return Ok(new LoginResponse(dto.Jwt));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, 
        CancellationToken cancellationToken = default)
    {
        var confirmationCode = await authService.RegisterAsync(
            new(request.Email), cancellationToken);

        await emailService.SendRegistrationConfirmationAsync(
            request.Email, confirmationCode, cancellationToken);

        return Ok($"Confirmation email sent to {request.Email}");
    }

    [HttpPost("register/confirm")]
    public async Task<IActionResult> ConfirmRegistration(
        [FromBody] RegistrationConfirmationRequest request, 
        CancellationToken cancellationToken = default)
    {
        var dto = await authService.ConfirmRegistrationAsync(new(
            ConfirmationCode: request.ConfirmationCode, 
            Username: request.Username,
            Password: request.Password), 
            cancellationToken);

        return Ok(new RegistrationConfirmationResponse(
            UserId: dto.UserId.Value,
            Username: dto.Username));
    }
}
