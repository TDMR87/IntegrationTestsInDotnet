namespace Bloqqer.Services.Dto;

public record RegistrationConfirmationRequestDto(string ConfirmationCode, string Username, string Password);
