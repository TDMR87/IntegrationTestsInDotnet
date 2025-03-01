namespace Bloqqer.Services.Dto;

public record UserRegistrationConfirmationCreateDto(
    string Email,
    string ConfirmationCode,
    DateTime ExpiresUtc
);