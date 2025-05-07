namespace publish_ats.cli;

/// <summary>
/// A static class to process and parse command-line input arguments for the publish-ats CLI tool.
/// </summary>
internal static class ProcessCliInput
{
    // Supported output formats for the CLI tool.
    private static readonly string[] SupportedFormats = { "pdf", "docx", "doc", "md" };

    /// <summary>
    /// Parses the command-line arguments and returns a populated <see cref="CommandLineOptions"/> object.
    /// </summary>
    /// <param name="args">The array of command-line arguments.</param>
    /// <returns>A <see cref="CommandLineOptions"/> object containing the parsed options.</returns>
    internal static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        // Iterate through all arguments to parse them.
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();

            // Parse input file path.
            if (arg is "-i" or "--input" && i + 1 < args.Length)
                options.InputFilePath = args[++i];
            // Parse output file path.
            else if (arg is "-o" or "--output" && i + 1 < args.Length)
                options.OutputFilePath = args[++i];
            // Parse output formats and validate them.
            else if (arg is "-f" or "--format" && i + 1 < args.Length)
                options.OutputFormats.AddRange(
                    args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(format => format.Trim().ToLower())
                        .Where(SupportedFormats.Contains)
                );
            // Enable ATS optimization if the flag is present.
            else if (arg is "-a" or "--ats")
                options.OptimizeForAts = true;
            // Display help message if the help flag is present.
            else if (arg is "-h" or "--help")
                Help.PrintToConsole();
            // Handle the case where the first argument is the input file path without a flag.
            else if (i == 0 && !arg.StartsWith("-"))
                options.InputFilePath = args[i];
            // Warn about unknown options.
            else
                Console.WriteLine($"Warning: Unknown option '{args[i]}'");
        }

        return options;
    }
}