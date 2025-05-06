namespace publish_ats.cli;

/// <summary>
/// Provides methods for importing input files and converting them to a supported format.
/// </summary>
internal static class Import
{
    /// <summary>
    /// Reads the input file and converts it to a string. 
    /// If the file is a Word document, it is converted to Markdown.
    /// </summary>
    /// <param name="inputFilePath">The path to the input file.</param>
    /// <returns>
    /// The content of the file as a string. If the file is a Word document, 
    /// the content is returned in Markdown format.
    /// </returns>
    internal static string InputFile(string inputFilePath)
    {
        // Get the file extension in lowercase.
        var ext = Path.GetExtension(inputFilePath).ToLowerInvariant();

        // Check if the file is a Markdown or Word document.
        var isMarkdown = ext == ".md";
        var isWord = ext is ".docx" or ".doc";

        // Warn the user if the file is neither Markdown nor Word format.
        if (!isMarkdown && !isWord)
            Console.WriteLine("Warning: The file is neither Markdown nor Word format.");

        // Convert Word documents to Markdown; otherwise, read the file content as text.
        return isWord ? cli.Convert.ToMarkdown(inputFilePath) : File.ReadAllText(inputFilePath);
    }
}