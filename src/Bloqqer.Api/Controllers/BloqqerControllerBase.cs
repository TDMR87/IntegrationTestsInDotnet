namespace Bloqqer.Api.Controllers;

[ApiController]
[Authorize]
public class BloqqerControllerBase : ControllerBase
{
    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue("userid")
         ?? throw new BloqqerUnauthorizedException("Claim type 'userid' not found."));
}
