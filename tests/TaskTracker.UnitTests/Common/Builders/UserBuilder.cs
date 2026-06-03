using TaskTracker.Domain.Entities;

namespace TaskTracker.UnitTests.Common.Builders;

public class UserBuilder
{
    private string _email = "alice@example.com";
    private string _displayName = "Alice";

    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithDisplayName(string displayName) { _displayName = displayName; return this; }

    public User Build() => new(_email, _displayName);
}
