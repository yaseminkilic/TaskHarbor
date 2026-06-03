using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Users.Dtos;
using TaskTracker.Domain.Entities;
using ValidationException = TaskTracker.Application.Common.Exceptions.ValidationException;

namespace TaskTracker.Application.Users.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UserService(
        IUserRepository users,
        IUnitOfWork uow,
        IValidator<CreateUserRequest> createValidator)
    {
        _users = users;
        _uow = uow;
        _createValidator = createValidator;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(normalizedEmail, ct))
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");

        var user = new User(request.Email, request.DisplayName);
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return UserDto.FromEntity(user);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await _users.ListAsync(ct);
        return users.Select(UserDto.FromEntity).ToList();
    }
}
