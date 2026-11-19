using System.Text.Json;

namespace _7WondersGame.Cli.Common;

/// <summary>Reading text values out of the JSON files and columns the program writes.</summary>
internal static class JsonText
{
    /// <summary>The text of a JSON string value. Throws for any other kind of value, including null.</summary>
    public static string StringOf(JsonElement value)
    {
        string? text = null;
        if (value.ValueKind == JsonValueKind.String)
        {
            text = value.GetString();
        }
        if (text == null)
        {
            throw new InvalidDataException($"Expected a JSON string, found {value.ValueKind}: {value.GetRawText()}");
        }
        return text;
    }
}
