namespace HostMe.Host.Models;

public record RegisterResponse(Guid Id, string Username, string Email, DateTime CreatedAt);
