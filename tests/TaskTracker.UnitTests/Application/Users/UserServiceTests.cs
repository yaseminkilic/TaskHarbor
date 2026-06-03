using FluentAssertions;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Users.Dtos;
using TaskTracker.Application.Users.Services;
using TaskTracker.Application.Users.Validators;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Application.Fixtures;

namespace TaskTracker.UnitTests.Application.Users;

public class UserServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fx = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_fx.Users, _fx.UnitOfWork, new CreateUserRequestValidator());
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task CreateAsync_PersistsUser_AndReturnsDto()
    {
        var dto = await _sut.CreateAsync(new CreateUserRequest("alice@example.com", "Alice"));

        dto.Email.Should().Be("alice@example.com");
        dto.DisplayName.Should().Be("Alice");
        dto.Id.Should().NotBe(Guid.Empty);

        var persisted = await _fx.Users.GetByIdAsync(dto.Id);
        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_IsCaseInsensitive_ThrowsConflict()
    {
        await _sut.CreateAsync(new CreateUserRequest("Foo@Bar.com", "Foo"));

        var act = () => _sut.CreateAsync(new CreateUserRequest("foo@bar.com", "Other"));

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*foo@bar.com*");
    }

    [Theory]
    [InlineData("", "Alice")]                  // empty email
    [InlineData("not-an-email", "Alice")]      // bad format
    [InlineData("alice@example.com", "")]      // empty displayName
    public async Task CreateAsync_InvalidRequest_ThrowsValidation(string email, string displayName)
    {
        var act = () => _sut.CreateAsync(new CreateUserRequest(email, displayName));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Entity.Should().Be(nameof(User));
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAtUtc()
    {
        await _sut.CreateAsync(new CreateUserRequest("a@example.com", "A"));
        await Task.Delay(5);  // ensure distinct CreatedAtUtc
        await _sut.CreateAsync(new CreateUserRequest("b@example.com", "B"));

        var list = await _sut.ListAsync();

        list.Should().HaveCount(2);
        list[0].Email.Should().Be("a@example.com");
        list[1].Email.Should().Be("b@example.com");
    }
}
