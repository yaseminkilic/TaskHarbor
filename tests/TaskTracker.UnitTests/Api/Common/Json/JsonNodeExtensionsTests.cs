using System.Text.Json.Nodes;
using FluentAssertions;
using TaskTracker.Api.Common.Json;

namespace TaskTracker.UnitTests.Api.Common.Json;

public class JsonNodeExtensionsTests
{
    // ---------- TryGetString / GetStringOrNull / GetRequiredString ----------

    [Fact]
    public void TryGetString_ReturnsTrue_WhenPropertyExistsWithValue()
    {
        var node = JsonNode.Parse("""{ "name": "alice" }""");

        var found = node.TryGetString("name", out var value);

        found.Should().BeTrue();
        value.Should().Be("alice");
    }

    [Fact]
    public void TryGetString_ReturnsFalse_WhenKeyMissing()
    {
        var node = JsonNode.Parse("""{ "other": "x" }""");

        var found = node.TryGetString("name", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetString_ReturnsFalse_WhenValueIsJsonNull()
    {
        var node = JsonNode.Parse("""{ "name": null }""");

        var found = node.TryGetString("name", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetString_ReturnsFalse_WhenNodeIsNull()
    {
        JsonNode? node = null;

        var found = node.TryGetString("name", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetString_ReturnsFalse_WhenNodeIsNotObject()
    {
        var node = JsonNode.Parse("""["a","b"]""");

        var found = node.TryGetString("name", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void GetStringOrNull_ReturnsValue_WhenPresent()
    {
        var node = JsonNode.Parse("""{ "name": "alice" }""");

        node.GetStringOrNull("name").Should().Be("alice");
    }

    [Fact]
    public void GetStringOrNull_ReturnsNull_WhenMissing()
    {
        var node = JsonNode.Parse("""{ }""");

        node.GetStringOrNull("name").Should().BeNull();
    }

    [Fact]
    public void GetRequiredString_ReturnsValue_WhenPresent()
    {
        var node = JsonNode.Parse("""{ "name": "alice" }""");

        node.GetRequiredString("name").Should().Be("alice");
    }

    [Fact]
    public void GetRequiredString_Throws_WhenMissing()
    {
        var node = JsonNode.Parse("""{ }""");

        var act = () => node.GetRequiredString("name");

        act.Should().Throw<InvalidOperationException>().WithMessage("*'name'*");
    }

    [Fact]
    public void GetRequiredString_Throws_WhenJsonNull()
    {
        var node = JsonNode.Parse("""{ "name": null }""");

        var act = () => node.GetRequiredString("name");

        act.Should().Throw<InvalidOperationException>();
    }

    // ---------- TryGetGuid / GetGuidOrNull / GetRequiredGuid ----------

    [Fact]
    public void TryGetGuid_ReturnsTrue_ForValidGuidString()
    {
        var id = Guid.NewGuid();
        var node = JsonNode.Parse($$"""{ "id": "{{id}}" }""");

        var found = node.TryGetGuid("id", out var value);

        found.Should().BeTrue();
        value.Should().Be(id);
    }

    [Theory]
    [InlineData("""{ "id": "not-a-guid" }""")]
    [InlineData("""{ "id": "" }""")]
    [InlineData("""{ "other": "x" }""")]
    [InlineData("""{ "id": null }""")]
    public void TryGetGuid_ReturnsFalse_ForInvalidOrMissing(string json)
    {
        var node = JsonNode.Parse(json);

        var found = node.TryGetGuid("id", out var value);

        found.Should().BeFalse();
        value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetGuidOrNull_ReturnsGuid_WhenValid()
    {
        var id = Guid.NewGuid();
        var node = JsonNode.Parse($$"""{ "id": "{{id}}" }""");

        node.GetGuidOrNull("id").Should().Be(id);
    }

    [Fact]
    public void GetGuidOrNull_ReturnsNull_WhenInvalid()
    {
        var node = JsonNode.Parse("""{ "id": "nope" }""");

        node.GetGuidOrNull("id").Should().BeNull();
    }

    [Fact]
    public void GetRequiredGuid_ReturnsGuid_WhenValid()
    {
        var id = Guid.NewGuid();
        var node = JsonNode.Parse($$"""{ "id": "{{id}}" }""");

        node.GetRequiredGuid("id").Should().Be(id);
    }

    [Fact]
    public void GetRequiredGuid_Throws_WhenInvalid()
    {
        var node = JsonNode.Parse("""{ "id": "nope" }""");

        var act = () => node.GetRequiredGuid("id");

        act.Should().Throw<InvalidOperationException>().WithMessage("*'id'*");
    }

    // ---------- TryGetInt / GetIntOrNull / GetRequiredInt ----------

    [Fact]
    public void TryGetInt_ReturnsTrue_ForIntegerNumber()
    {
        var node = JsonNode.Parse("""{ "count": 42 }""");

        var found = node.TryGetInt("count", out var value);

        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Theory]
    [InlineData("""{ "count": "42" }""")]   // string, not number
    [InlineData("""{ "count": 1.5 }""")]    // not integer
    [InlineData("""{ "other": 1 }""")]      // missing
    [InlineData("""{ "count": null }""")]   // null
    public void TryGetInt_ReturnsFalse_ForInvalidOrMissing(string json)
    {
        var node = JsonNode.Parse(json);

        var found = node.TryGetInt("count", out var value);

        found.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void GetIntOrNull_ReturnsValue_WhenInteger()
    {
        var node = JsonNode.Parse("""{ "count": 7 }""");

        node.GetIntOrNull("count").Should().Be(7);
    }

    [Fact]
    public void GetIntOrNull_ReturnsNull_WhenMissing()
    {
        var node = JsonNode.Parse("""{ }""");

        node.GetIntOrNull("count").Should().BeNull();
    }

    [Fact]
    public void GetRequiredInt_ReturnsValue_WhenInteger()
    {
        var node = JsonNode.Parse("""{ "count": 7 }""");

        node.GetRequiredInt("count").Should().Be(7);
    }

    [Fact]
    public void GetRequiredInt_Throws_WhenMissing()
    {
        var node = JsonNode.Parse("""{ }""");

        var act = () => node.GetRequiredInt("count");

        act.Should().Throw<InvalidOperationException>().WithMessage("*'count'*");
    }

    // ---------- TryGetBool / GetBoolOrNull ----------

    [Theory]
    [InlineData("""{ "flag": true }""", true)]
    [InlineData("""{ "flag": false }""", false)]
    public void TryGetBool_ReturnsTrue_ForBooleanValue(string json, bool expected)
    {
        var node = JsonNode.Parse(json);

        var found = node.TryGetBool("flag", out var value);

        found.Should().BeTrue();
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{ "flag": "true" }""")]  // string, not bool
    [InlineData("""{ "flag": 1 }""")]       // number, not bool
    [InlineData("""{ "other": true }""")]   // missing
    [InlineData("""{ "flag": null }""")]    // null
    public void TryGetBool_ReturnsFalse_ForInvalidOrMissing(string json)
    {
        var node = JsonNode.Parse(json);

        var found = node.TryGetBool("flag", out var value);

        found.Should().BeFalse();
        value.Should().BeFalse();
    }

    [Fact]
    public void GetBoolOrNull_ReturnsValue_WhenBool()
    {
        var node = JsonNode.Parse("""{ "flag": true }""");

        node.GetBoolOrNull("flag").Should().BeTrue();
    }

    [Fact]
    public void GetBoolOrNull_ReturnsNull_WhenMissing()
    {
        var node = JsonNode.Parse("""{ }""");

        node.GetBoolOrNull("flag").Should().BeNull();
    }
}
