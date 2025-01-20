namespace Bloqqer.Services.Dto;

public record LoginResponseDto(UserDto LoggedInUser, string Jwt);
