namespace publish_ats.cli;

/// <summary>
///     Provides validation methods for input file paths and output formats in the CLI application.
/// </summary>
internal static class Validate
{
    /// <summary>
    ///     Validates the input file path. Ensures that the file path is not null or empty
    ///     and that the file exists at the specified path. If validation fails, the program
    ///     prints an error message and exits.
    /// </summary>
    /// <param name="inputFilePath">The input file path to validate.</param>
    internal static void FileNameAndPath(string? inputFilePath)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            Console.Error.WriteLine("Error: Input file path is required.");
            Help.PrintToConsole();
            Environment.Exit(1);
        }

        if (File.Exists(inputFilePath)) return;
        // Raise an error if the file does not exist
        Console.Error.WriteLine($"Error: File not found at path '{inputFilePath}'.");
        Environment.Exit(1);
    }

    /// <summary>
    ///     Validates the list of output formats. Ensures that at least one output format
    ///     is specified. If validation fails, the program prints an error message and exits.
    /// </summary>
    /// <param name="outputFormats">The list of output formats to validate.</param>
    internal static void OutputFormats(List<string> outputFormats)
    {
        if (outputFormats.Count != 0) return;
        // Raise an error if no output formats are specified
        Console.Error.WriteLine("Error: At least one output format must be specified.");
        Help.PrintToConsole();
        Environment.Exit(1);
    }
}