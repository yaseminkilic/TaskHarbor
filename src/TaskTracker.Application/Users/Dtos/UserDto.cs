using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Users.Dtos;

public record UserDto(Guid Id, string Email, string DisplayName, DateTime CreatedAtUtc)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc);
}
