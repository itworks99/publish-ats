using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace publish_ats.cli;

internal static partial class Convert
{
    internal static string ToMarkdown(string inputPath)
    {
        Console.WriteLine("Converting Word document to Markdown...");
        var md = ConvertWordToMarkdown(inputPath);
        var mdPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? "",
            Path.GetFileNameWithoutExtension(inputPath) + "_extracted.md");
        Export.SaveMarkdownFile(mdPath, md);
        return md;
    }

    internal static void ToPdf(string htmlContent, string outputPath)
    {
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var doc = new Document(pdf);
        doc.Add(new Paragraph(StripHtmlTags(htmlContent)));
        Console.WriteLine($"Converted to PDF: {outputPath}");
    }

    internal static void ToWord(string htmlContent, string outputPath)
    {
        using var wordDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());
        var para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
        para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(StripHtmlTags(htmlContent))));
        Console.WriteLine($"Converted to Word: {outputPath}");
    }

    private static string StripHtmlTags(string html) => MyRegex().Replace(html, string.Empty);

    private static string ConvertWordToMarkdown(string inputPath)
    {
        var sb = new StringBuilder();

        using (var wordDoc = WordprocessingDocument.Open(inputPath, false))
        {
            var mainPart = wordDoc.MainDocumentPart ??
                           throw new InvalidOperationException(
                               "The Word document appears to be empty or corrupted.");
            var body = mainPart.Document.Body ??
                       throw new InvalidOperationException("The Word document does not have a body element.");

            foreach (var element in body.ChildElements)
            {
                switch (element)
                {
                    case DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph:
                        AppendParagraphToMarkdown(paragraph, sb);
                        break;
                    case DocumentFormat.OpenXml.Wordprocessing.Table table:
                        AppendTableToMarkdown(table, sb);
                        break;
                    default:
                    {
                        if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph listPara &&
                            listPara.ParagraphProperties?.NumberingProperties != null)
                            sb.AppendLine("* " + string.Join(" ", listPara.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)));
                        break;
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendParagraphToMarkdown(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph, StringBuilder sb)
    {
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (style != null && style.StartsWith("Heading") && int.TryParse(style.AsSpan(7), out var headingLevel) &&
            headingLevel is > 0 and <= 6)
        {
            sb.Append(new string('#', headingLevel) + " ");
        }

        foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
        {
            var textContent = string.Join("", run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
            if (run.RunProperties?.Bold != null)
                sb.Append($"**{textContent}**");
            else if (run.RunProperties?.Italic != null)
                sb.Append($"*{textContent}*");
            else
                sb.Append(textContent);
        }

        sb.AppendLine("\n");
    }

    private static void AppendTableToMarkdown(DocumentFormat.OpenXml.Wordprocessing.Table table, StringBuilder sb)
    {
        foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
        {
            sb.Append("| ");
            foreach (var cell in row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>())
            {
                sb.Append(string.Join(" ", cell.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)) + " | ");
            }

            sb.AppendLine();

            if (row != table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>().FirstOrDefault()) continue;
            // Add separator line for the header
            sb.Append("| ");
            sb.Append(string.Join("", row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>().Select(_ => "--- | ")));
            sb.AppendLine();
        }

        sb.AppendLine();
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex MyRegex();
}