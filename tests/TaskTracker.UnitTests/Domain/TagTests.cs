using FluentAssertions;
using TaskTracker.Domain.Common;
using TaskTracker.Domain.Entities;

namespace TaskTracker.UnitTests.Domain;

public class TagTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        var act = () => new Tag(name);

        act.Should().Throw<DomainException>().WithMessage("*Tag*");
    }

    [Theory]
    [InlineData("Urgent", "urgent")]
    [InlineData("  Backend  ", "backend")]
    [InlineData("FOO", "foo")]
    public void Constructor_NormalizesName_ToLowerAndTrim(string input, string expected)
    {
        var tag = new Tag(input);

        tag.Name.Should().Be(expected);
        tag.Id.Should().NotBe(Guid.Empty);
    }
}
