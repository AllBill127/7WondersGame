namespace _7WondersGame.Cli.Common;

/// <summary>Paths of the files the program reads and writes.</summary>
internal static class FilePaths
{
    /// <summary>
    /// The folder <paramref name="path"/> is in ("" for a bare file name). Throws for a path that has no folder
    /// part at all, such as a drive root.
    /// </summary>
    public static string DirectoryOf(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory == null)
        {
            throw new ArgumentException($"'{path}' is not a path to a file.", nameof(path));
        }
        return directory;
    }
}
