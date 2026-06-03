using System.Text.Json.Nodes;

namespace TaskTracker.Api.Common.Json;

public static class JsonNodeExtensions
{
    public static bool TryGetString(this JsonNode? node, string key, out string? value)
    {
        value = null;
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(key, out var prop) || prop is null)
            return false;

        value = prop.GetValue<string?>();
        return value is not null;
    }

    public static string? GetStringOrNull(this JsonNode? node, string key)
        => node.TryGetString(key, out var v) ? v : null;

    public static string GetRequiredString(this JsonNode? node, string key)
        => node.TryGetString(key, out var v) && v is not null
            ? v
            : throw new InvalidOperationException($"Required string property '{key}' is missing or null.");

    public static bool TryGetGuid(this JsonNode? node, string key, out Guid value)
    {
        value = default;
        return node.TryGetString(key, out var raw)
            && raw is not null
            && Guid.TryParse(raw, out value);
    }

    public static Guid? GetGuidOrNull(this JsonNode? node, string key)
        => node.TryGetGuid(key, out var v) ? v : null;

    public static Guid GetRequiredGuid(this JsonNode? node, string key)
        => node.TryGetGuid(key, out var v)
            ? v
            : throw new InvalidOperationException($"Required Guid property '{key}' is missing or not a valid Guid.");

    public static bool TryGetInt(this JsonNode? node, string key, out int value)
    {
        value = 0;
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(key, out var prop) || prop is null)
            return false;

        try
        {
            value = prop.GetValue<int>();
            return true;
        }
        catch (FormatException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public static int? GetIntOrNull(this JsonNode? node, string key)
        => node.TryGetInt(key, out var v) ? v : null;

    public static int GetRequiredInt(this JsonNode? node, string key)
        => node.TryGetInt(key, out var v)
            ? v
            : throw new InvalidOperationException($"Required int property '{key}' is missing or not a number.");

    public static bool TryGetBool(this JsonNode? node, string key, out bool value)
    {
        value = false;
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(key, out var prop) || prop is null)
            return false;

        try
        {
            value = prop.GetValue<bool>();
            return true;
        }
        catch (FormatException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public static bool? GetBoolOrNull(this JsonNode? node, string key)
        => node.TryGetBool(key, out var v) ? v : null;
}
