namespace publish_ats.cli;

/// <summary>
///     Provides a static method to display help information for the Publish-ATS CLI tool.
/// </summary>
internal static class Help
{
    /// <summary>
    ///     Prints the help message to the console, including usage instructions, options, and examples.
    /// </summary>
    internal static void PrintToConsole()
    {
        Console.WriteLine("Enhance-ATS: Publish CV into ATS-friendly formats");
        Console.WriteLine("=================================================");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  publish-ats [options] <input_file.md or input_file.docx>");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -i, --input <file>     Input markdown or Word file path");
        Console.WriteLine("  -o, --output <file>    Output file path (optional)");
        Console.WriteLine("  -f, --format <formats> Output formats (comma-separated: pdf,docx,doc,md)");
        Console.WriteLine("  -a, --ats              Optimize for Applicant Tracking Systems");
        Console.WriteLine("  -h, --help             Display this help message");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  publish-ats resume.md -f pdf,docx -a");
        Console.WriteLine("  publish-ats -i resume.docx -o output_resume -f pdf,md");
    }
}