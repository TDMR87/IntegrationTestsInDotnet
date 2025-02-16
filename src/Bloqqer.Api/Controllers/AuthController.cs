namespace Bloqqer.Api.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var dto = await authService.LoginAsync(new(
            Email: request.Email,
            Password: request.Password),
            cancellationToken
        );

        return Ok(new LoginResponse(dto.Jwt));
    }
}
