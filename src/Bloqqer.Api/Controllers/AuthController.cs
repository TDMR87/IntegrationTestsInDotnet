namespace Bloqqer.Api.Controllers;

[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var dto = await authService.LoginAsync(new(
            Email: request.Username,
            Password: request.Password),
            cancellationToken
        );

        return Ok(new LoginResponse(dto.Jwt));
    }
}
