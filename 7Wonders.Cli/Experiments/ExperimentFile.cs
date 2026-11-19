namespace _7Wonders.Cli.Experiments;

/// <summary>What an experiment file holds: its options as command-line text, and its description.</summary>
internal sealed class ExperimentFile
{
    /// <summary>The file's <paramref name="options"/> and <paramref name="description"/>.</summary>
    public ExperimentFile(Dictionary<string, string> options, string? description)
    {
        Options = options;
        Description = description;
    }

    /// <summary>Each option the file gives, by name (any case), as it would be written on the command line.</summary>
    public Dictionary<string, string> Options { get; }

    /// <summary>The file's description, if it has one.</summary>
    public string? Description { get; }
}
