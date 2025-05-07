namespace publish_ats.cli;

/// <summary>
///     Represents the command-line options for the Publish-ATS CLI tool.
/// </summary>
internal class CommandLineOptions
{
    /// <summary>
    ///     Gets or sets the input file path provided by the user.
    /// </summary>
    public string? InputFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the output file path specified by the user (optional).
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    ///     Gets the list of output formats specified by the user (e.g., pdf, docx, md).
    /// </summary>
    public List<string> OutputFormats { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether to optimize the output for Applicant Tracking Systems (ATS).
    /// </summary>
    public bool OptimizeForAts { get; set; }
}