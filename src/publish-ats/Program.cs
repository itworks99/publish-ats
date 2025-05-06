using JetBrains.Annotations;
using Markdig;
using publish_ats.cli;
using publish_ats.nlp;
using Console = System.Console;

namespace publish_ats;

/// <summary>
/// The main entry point for the application.
/// Handles command-line input, validates options, processes the input file, 
/// and exports the content in the specified formats.
/// </summary>
[UsedImplicitly]
internal class Program
{
    /// <summary>
    /// The main method that executes the program logic.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    private static void Main(string[] args)
    {
        // Parse command-line arguments into options.
        var options = ProcessCliInput.Parse(args);

        // Validate the input file path and output formats.
        Validate.FileNameAndPath(options.InputFilePath);
        Validate.OutputFormats(options.OutputFormats);

        // Exit if no input file path is provided.
        if (options.InputFilePath == null) return;

        try
        {
            // Read the input file and convert it to Markdown.
            var markdown = Import.InputFile(options.InputFilePath);

            // Optimize the Markdown for ATS if the option is enabled.
            if (options.OptimizeForAts)
            {
                markdown = Nlp.OptimizeForAts(markdown);
                // Save the ATS-optimized Markdown to a file.
                Export.SaveMarkdownFile(Export.PrepareAtsOptimizedFilePath(options.InputFilePath), markdown);
            }

            // Export the content in the specified formats.
            Export.Files(options, markdown, Markdown.ToHtml(markdown));
        }
        catch (Exception ex)
        {
            // Handle any exceptions and exit with an error code.
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}