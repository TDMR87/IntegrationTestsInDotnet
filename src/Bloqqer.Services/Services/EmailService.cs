namespace Bloqqer.Services.Services;

public interface IEmailService
{
    Task SendRegistrationConfirmationAsync(
        string email, 
        string confirmationCode, 
        CancellationToken cancellationToken = default);
}

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendRegistrationConfirmationAsync(
        string email, 
        string confirmationCode, 
        CancellationToken cancellationToken = default)
    {
        var url = configuration["Frontend:RegistrationConfirmationUrl"];
        throw new NotImplementedException();
    }
}