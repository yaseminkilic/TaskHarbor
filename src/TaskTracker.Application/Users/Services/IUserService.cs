using TaskTracker.Application.Users.Dtos;

namespace TaskTracker.Application.Users.Services;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);
}
