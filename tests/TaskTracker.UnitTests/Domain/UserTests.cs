using FluentAssertions;
using TaskTracker.Domain.Common;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Common.Builders;

namespace TaskTracker.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("noatsign")]
    [InlineData("foo@bar")]            // missing dot
    [InlineData("foo bar@baz.com")]    // space in local part
    [InlineData("@bar.com")]           // empty local part
    [InlineData("foo@.com")]           // empty domain label
    public void Constructor_WithInvalidEmail_Throws(string email)
    {
        var act = () => new User(email, "Alice");

        act.Should().Throw<DomainException>().WithMessage("*Email*");
    }

    [Fact]
    public void Constructor_NormalizesEmail_LowercasesAndTrims()
    {
        var user = new User("  Foo@Bar.COM  ", "Alice");

        user.Email.Should().Be("foo@bar.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDisplayName_Throws(string displayName)
    {
        var act = () => new User("alice@example.com", displayName);

        act.Should().Throw<DomainException>().WithMessage("*DisplayName*");
    }

    [Fact]
    public void Constructor_TrimsDisplayName()
    {
        var user = new UserBuilder().WithDisplayName("  Alice  ").Build();

        user.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public void Constructor_AssignsIdAndCreatedAtUtc()
    {
        var before = DateTime.UtcNow;
        var user = new UserBuilder().Build();
        var after = DateTime.UtcNow;

        user.Id.Should().NotBe(Guid.Empty);
        user.CreatedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmpty_Throws(string newName)
    {
        var user = new UserBuilder().Build();

        var act = () => user.Rename(newName);

        act.Should().Throw<DomainException>().WithMessage("*DisplayName*");
    }

    [Fact]
    public void Rename_TrimsAndUpdates()
    {
        var user = new UserBuilder().Build();

        user.Rename("  Bob  ");

        user.DisplayName.Should().Be("Bob");
    }
}
