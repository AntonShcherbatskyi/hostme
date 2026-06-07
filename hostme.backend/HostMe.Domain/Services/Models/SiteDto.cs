namespace HostMe.Domain.Services.Models;

public record SiteDto(Guid Id, Guid UserId, string Name, string Url, DateTime CreatedAt);
