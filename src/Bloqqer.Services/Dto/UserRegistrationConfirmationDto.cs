namespace Bloqqer.Services.Dto;

public record UserRegistrationConfirmationDto(
    UserRegistrationConfirmationId Id,
    string Email, 
    string ConfirmationCode, 
    DateTime ExpiresUtc
);