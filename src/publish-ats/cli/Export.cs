namespace publish_ats.cli;

/// <summary>
/// Provides methods for exporting files in various formats and preparing file paths.
/// </summary>
internal static class Export
{
    /// <summary>
    /// Exports files in the specified formats based on the provided options.
    /// </summary>
    /// <param name="options">The command-line options containing input, output, and format details.</param>
    /// <param name="markdown">The Markdown content to be exported.</param>
    /// <param name="html">The HTML content to be exported.</param>
    internal static void Files(CommandLineOptions options, string markdown, string html)
    {
        foreach (var format in options.OutputFormats)
        {
            if (options.InputFilePath == null) continue;

            var output = PrepareOutputFilePath(options.InputFilePath, format, options.OutputFilePath);
            if (string.IsNullOrEmpty(output)) continue;

            switch (format.ToLowerInvariant())
            {
                case "pdf":
                    Convert.ToPdf(html, output);
                    break;
                case "doc":
                case "docx":
                    Convert.ToWord(html, output);
                    break;
                case "md":
                    SaveMarkdownFile(output, markdown);
                    break;
                default:
                    Console.WriteLine($"Warning: Unsupported format '{format}' skipped.");
                    break;
            }
        }
    }

    /// <summary>
    /// Prepares the output file path based on the input path, format, and optional custom output path.
    /// </summary>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="format">The desired output format (e.g., pdf, docx, md).</param>
    /// <param name="customOutputPath">The custom output file path, if specified.</param>
    /// <returns>The prepared output file path.</returns>
    private static string PrepareOutputFilePath(string inputPath, string format, string? customOutputPath)
    {
        if (!string.IsNullOrEmpty(customOutputPath))
            return Path.HasExtension(customOutputPath) ? customOutputPath : $"{customOutputPath}.{format}";

        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var outputPath = Path.Combine(directory, $"{fileName}.{format}");

        if (File.Exists(outputPath))
            Console.WriteLine($"Warning: Overwriting existing file {outputPath}");

        return outputPath;
    }

    /// <summary>
    /// Saves the provided Markdown content to the specified file path.
    /// </summary>
    /// <param name="path">The file path where the Markdown content will be saved.</param>
    /// <param name="content">The Markdown content to save.</param>
    internal static void SaveMarkdownFile(string path, string content)
    {
        File.WriteAllText(path, content);
        Console.WriteLine($"Saved: {path}");
    }

    /// <summary>
    /// Prepares a file path for an ATS-optimized Markdown file based on the input file path.
    /// </summary>
    /// <param name="inputFilePath">The input file path.</param>
    /// <returns>The prepared file path for the ATS-optimized Markdown file.</returns>
    internal static string PrepareAtsOptimizedFilePath(string inputFilePath)
    {
        var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
        return Path.Combine(directory, $"{fileName}_ats_optimized.md");
    }
}