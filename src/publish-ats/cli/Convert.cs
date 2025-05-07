using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace publish_ats.cli;

/// <summary>
///     Provides methods for converting documents between different formats such as Markdown, PDF, and Word.
/// </summary>
internal static partial class Convert
{
    private static bool _hasProcessedFirstHeading;

    /// <summary>
    ///     Converts a Word document to Markdown format.
    /// </summary>
    /// <param name="inputPath">The path to the input Word document.</param>
    /// <returns>The converted Markdown content as a string.</returns>
    internal static string ToMarkdown(string inputPath)
    {
        Console.WriteLine("Converting Word document to Markdown...");
        _hasProcessedFirstHeading = false;

        var markdown = ConvertWordToMarkdown(inputPath);
        var outputPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? "",
            Path.GetFileNameWithoutExtension(inputPath) + "_extracted.md");
        Export.SaveMarkdownFile(outputPath, markdown);

        return markdown;
    }

    /// <summary>
    ///     Converts the content of a Word document to Markdown format.
    /// </summary>
    /// <param name="inputPath">The path to the input Word document.</param>
    /// <returns>The converted Markdown content as a string.</returns>
    private static string ConvertWordToMarkdown(string inputPath)
    {
        var sb = new StringBuilder();

        using var wordDoc = WordprocessingDocument.Open(inputPath, false);
        var body = wordDoc.MainDocumentPart?.Document.Body ??
                   throw new InvalidOperationException("Invalid Word document.");

        foreach (var element in body.ChildElements)
            switch (element)
            {
                case Paragraph paragraph:
                    ProcessParagraph(paragraph, sb);
                    break;
                case Table table:
                    AppendTableToMarkdown(table, sb);
                    break;
            }

        return sb.ToString();
    }

    /// <summary>
    ///     Processes a paragraph element and appends its content to the Markdown output.
    /// </summary>
    /// <param name="paragraph">The paragraph to process.</param>
    /// <param name="sb">The StringBuilder to append the Markdown content to.</param>
    private static void ProcessParagraph(Paragraph paragraph, StringBuilder sb)
    {
        if (paragraph.ParagraphProperties?.NumberingProperties != null)
        {
            sb.Append("* ");
            AppendParagraphContent(paragraph, sb);
            sb.AppendLine();
        }
        else
        {
            var text = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
            if (ContainsJobDatePattern(text)) SplitJobTitleAndDate(paragraph, sb);
            else AppendParagraphToMarkdown(paragraph, sb);
        }
    }

    /// <summary>
    ///     Appends a paragraph to the Markdown output, applying appropriate styles.
    /// </summary>
    /// <param name="paragraph">The paragraph to append.</param>
    /// <param name="sb">The StringBuilder to append the Markdown content to.</param>
    private static void AppendParagraphToMarkdown(Paragraph paragraph, StringBuilder sb)
    {
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        if (style == "Title" && !_hasProcessedFirstHeading)
        {
            sb.Append("# ");
            _hasProcessedFirstHeading = true;
        }
        else if (style?.StartsWith("Heading") == true && int.TryParse(style[7..], out var level) &&
                 level is > 0 and <= 6)
        {
            sb.Append(new string('#', level) + " ");
        }

        AppendParagraphContent(paragraph, sb);
        sb.AppendLine("\n");
    }

    /// <summary>
    ///     Appends the content of a paragraph to the Markdown output.
    /// </summary>
    /// <param name="paragraph">The paragraph to process.</param>
    /// <param name="sb">The StringBuilder to append the Markdown content to.</param>
    private static void AppendParagraphContent(Paragraph paragraph, StringBuilder sb)
    {
        foreach (var run in paragraph.Elements<Run>())
        {
            var text = string.Join("", run.Elements<Text>().Select(t => t.Text));
            if (string.IsNullOrEmpty(text)) continue;

            var isBold = run.RunProperties?.Bold != null;
            var isItalic = run.RunProperties?.Italic != null;

            if (isBold) sb.Append("**");
            if (isItalic) sb.Append("*");

            sb.Append(text);

            if (isBold) sb.Append("**");
            if (isItalic) sb.Append("*");
        }
    }

    /// <summary>
    ///     Splits a paragraph into a job title and date, appending them to the Markdown output.
    /// </summary>
    /// <param name="paragraph">The paragraph to process.</param>
    /// <param name="sb">The StringBuilder to append the Markdown content to.</param>
    private static void SplitJobTitleAndDate(Paragraph paragraph, StringBuilder sb)
    {
        var title = new StringBuilder();
        var date = new StringBuilder();
        var isInDatePart = false;

        foreach (var run in paragraph.Elements<Run>())
        {
            var text = string.Join("", run.Elements<Text>().Select(t => t.Text));
            if (string.IsNullOrEmpty(text)) continue;

            if (!isInDatePart && run.RunProperties?.Italic != null) isInDatePart = true;

            if (isInDatePart) date.Append(text);
            else title.Append(text);
        }

        sb.AppendLine($"#### {title.ToString().Trim()}");
        sb.AppendLine($"*{date.ToString().Trim()}*");
    }

    /// <summary>
    ///     Appends a table to the Markdown output.
    /// </summary>
    /// <param name="table">The table to process.</param>
    /// <param name="sb">The StringBuilder to append the Markdown content to.</param>
    private static void AppendTableToMarkdown(Table table, StringBuilder sb)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            sb.Append("| ");
            foreach (var cell in row.Elements<TableCell>())
                sb.Append(string.Join(" ", cell.Descendants<Text>().Select(t => t.Text)) + " | ");
            sb.AppendLine();

            if (row == table.Elements<TableRow>().First())
            {
                sb.Append("| ");
                sb.Append(string.Join("", row.Elements<TableCell>().Select(_ => "--- | ")));
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Checks if a text contains a job date pattern.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text contains a job date pattern; otherwise, false.</returns>
    private static bool ContainsJobDatePattern(string text)
    {
        return text.Contains(" – ") && MyRegex1().IsMatch(text);
    }

    /// <summary>
    ///     Regex to match job date patterns (e.g., "Jan 2020").
    /// </summary>
    [GeneratedRegex(
        @"\b(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4}")]
    private static partial Regex MyRegex1();

    /// <summary>
    ///     Converts HTML content to a PDF file.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <param name="output">The path to save the PDF file.</param>
    internal static void ToPdf(string html, string output)
    {
        Console.WriteLine("Converting to PDF...");

        try
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "pdf-template.html");
            if (!File.Exists(templatePath)) CreateDefaultTemplate(templatePath);

            var completeHtml = File.ReadAllText(templatePath).Replace("{{content}}", html);
            var tempHtmlPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".html");
            File.WriteAllText(tempHtmlPath, completeHtml);

            var browserFetcher = new BrowserFetcher();
            browserFetcher.DownloadAsync().GetAwaiter().GetResult();

            using var browser = Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = ["--no-sandbox"] })
                .GetAwaiter().GetResult();
            using var page = browser.NewPageAsync().GetAwaiter().GetResult();

            page.GoToAsync($"file://{tempHtmlPath}").GetAwaiter().GetResult();
            page.PdfAsync(output, new PdfOptions { Format = PaperFormat.A4, PrintBackground = true }).GetAwaiter()
                .GetResult();

            File.Delete(tempHtmlPath);
            Console.WriteLine($"Saved: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting to PDF: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Creates a default HTML template for PDF generation.
    /// </summary>
    /// <param name="templatePath">The path to save the template.</param>
    private static void CreateDefaultTemplate(string templatePath)
    {
        if (!File.Exists(templatePath)) Directory.CreateDirectory(Path.GetDirectoryName(templatePath) ?? "");
        const string defaultTemplate =
            @"<!DOCTYPE html><html><head><style>body{font-family:'IBM Plex Sans', sans-serif;}</style></head><body>{{content}}</body></html>";
        File.WriteAllText(templatePath, defaultTemplate);
    }

    /// <summary>
    ///     Converts HTML content to a Word document.
    /// </summary>
    /// <param name="html">The HTML content to convert.</param>
    /// <param name="output">The path to save the Word document.</param>
    internal static void ToWord(string html, string output)
    {
        Console.WriteLine("Converting to Word...");

        try
        {
            // Ensure HTML has proper body tags
            if (!html.Contains("<body>")) html = $"<html><body>{html}</body></html>";

            using var memoryStream = new MemoryStream();
            using var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document);

            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var converter = new HtmlToOpenXmlConverter(mainPart);
            var paragraphs = converter.Parse(html);

            if (paragraphs.Count == 0)
                // Fallback if no paragraphs were created
                mainPart.Document.Body.AppendChild(
                    new Paragraph(new Run(new Text("Content could not be properly converted from HTML."))));
            else
                foreach (var paragraph in paragraphs)
                    mainPart.Document.Body.AppendChild(paragraph);

            document.Save();
            File.WriteAllBytes(output, memoryStream.ToArray());
            Console.WriteLine($"Saved: {output}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting to Word: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Helper class to convert HTML content to OpenXml elements for Word documents.
    /// </summary>
    private class HtmlToOpenXmlConverter
    {
        private readonly MainDocumentPart _mainPart;

        public HtmlToOpenXmlConverter(MainDocumentPart mainPart)
        {
            _mainPart = mainPart;
        }

        public List<Paragraph> Parse(string html)
        {
            var paragraphs = new List<Paragraph>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // First try to get all body children
            var nodes = htmlDoc.DocumentNode.SelectNodes("//body/*");

            // If no nodes found, try to get all content
            if (nodes == null || !nodes.Any())
                nodes = htmlDoc.DocumentNode.SelectNodes(
                    "//*[not(self::html) and not(self::head) and not(self::body)]");

            // If still no nodes, create a paragraph from the entire text
            if (nodes == null || !nodes.Any())
            {
                var text = htmlDoc.DocumentNode.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                    paragraphs.Add(new Paragraph(new Run(new Text(HttpUtility.HtmlDecode(text)))));
                return paragraphs;
            }

            foreach (var node in nodes) ProcessNode(node, paragraphs);

            return paragraphs;
        }

        private static void ProcessNode(HtmlNode node, List<Paragraph> paragraphs)
        {
            switch (node.Name.ToLower())
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    paragraphs.Add(CreateHeadingParagraph(node));
                    break;
                case "p":
                    paragraphs.Add(CreateParagraph(node));
                    break;
                case "ul":
                case "ol":
                    ProcessList(node, paragraphs, node.Name == "ol");
                    break;
                case "div":
                    // Process div children
                    var children = node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).ToList();
                    if (children.Any())
                        foreach (var child in children)
                            ProcessNode(child, paragraphs);
                    else if (!string.IsNullOrWhiteSpace(node.InnerText)) paragraphs.Add(CreateParagraph(node));

                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(node.InnerText)) paragraphs.Add(CreateParagraph(node));
                    break;
            }
        }

        private static Paragraph CreateHeadingParagraph(HtmlNode node)
        {
            var level = int.Parse(node.Name[1..]);
            return new Paragraph(new Run(new Text(HttpUtility.HtmlDecode(node.InnerText))))
            {
                ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" })
            };
        }

        private static Paragraph CreateParagraph(HtmlNode node)
        {
            var decodedText = HttpUtility.HtmlDecode(node.InnerText);
            return new Paragraph(new Run(new Text(decodedText)));
        }

        private static void ProcessList(HtmlNode node, List<Paragraph> paragraphs, bool isOrdered)
        {
            var i = 1;
            var listItems = node.SelectNodes("./li");
            if (listItems == null) return;
            foreach (var item in listItems)
            {
                var decodedText = HttpUtility.HtmlDecode(item.InnerText);
                var text = isOrdered ? $"{i}. {decodedText}" : $"• {decodedText}";
                paragraphs.Add(new Paragraph(new Run(new Text(text))));
                i++;
            }
        }
    }
}