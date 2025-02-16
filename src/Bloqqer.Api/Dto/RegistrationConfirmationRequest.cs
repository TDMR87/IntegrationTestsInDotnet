namespace Bloqqer.Api.Dto;

public record RegistrationConfirmationRequest(string ConfirmationCode, string Username, string Password);
