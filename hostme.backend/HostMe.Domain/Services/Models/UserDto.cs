namespace HostMe.Domain.Services.Models;

public record UserDto(Guid Id, string Username, string Email, DateTime CreatedAt);
