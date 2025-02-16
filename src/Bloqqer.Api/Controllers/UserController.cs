namespace Bloqqer.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IUserService userService) : BloqqerControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userService.CreateAsync(new(
            Username: request.Username,
            Email: request.Email), 
            cancellationToken);

        return Ok(new UserResponse(
            Id: user.Id.Value,
            Username: user.Username,
            Email: user.Email));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetUserById(CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByIdAsync(new UserId(CurrentUserId), cancellationToken);

        return Ok(new UserResponse(
            Id: user.Id.Value,
            Username: user.Username,
            Email: user.Email));
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByIdAsync(new UserId(id), cancellationToken);

        return Ok(new UserResponse(
            Id: user.Id.Value,
            Username: user.Username,
            Email: user.Email));
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail([FromRoute] string email, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailAsync(email, cancellationToken);

        return Ok(new UserResponse(
            Id: user.Id.Value,
            Username: user.Username,
            Email: user.Email));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userService.UpdateAsync(new(
            UserId: new UserId(id),
            Username: request.Username,
            UpdatedById: new UserId(CurrentUserId)), cancellationToken);

        return Ok(new UserResponse(
            Id: user.Id.Value,
            Username: user.Username,
            Email: user.Email));
    }
}
