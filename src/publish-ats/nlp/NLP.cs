using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace publish_ats.nlp;

/// <summary>
/// Provides Natural Language Processing (NLP) utilities for optimizing markdown content for ATS (Applicant Tracking Systems).
/// </summary>
internal static class Nlp
{
    /// <summary>
    /// Optimizes the given markdown content for ATS by detecting relevant resume entities and
    /// highlighting them in bold. Additionally, a summary of detected entities is prepended to the markdown.
    /// </summary>
    /// <param name="markdown">The markdown content to be optimized.</param>
    /// <returns>The optimized markdown content with detected entities highlighted and summarized.</returns>
internal static string OptimizeForAts(string markdown)
{
    // First, sanitize the markdown to prepare it for processing
    string sanitizedMarkdown = SanitizeMarkdown(markdown);

    // Create sets to store categorized entities
    var skills = new HashSet<string>();
    var technologies = new HashSet<string>();
    var jobTitles = new HashSet<string>();
    var education = new HashSet<string>();
    
    // Define more precise patterns focused on resume keywords with word boundaries
    var patterns = new Dictionary<string, (string pattern, HashSet<string> category)>
    {
        { "Technologies", (@"\b(JavaScript|TypeScript|Python|Java|C#|\.NET|React|Angular|Node\.js|AWS|Azure|SQL|Docker|Kubernetes|PostgreSQL|MongoDB|Git|REST|GraphQL|CI/CD|Terraform|Kafka|Redis|Cassandra|NoSQL|DevOps|Go|R)\b", technologies) },
        { "JobTitles", (@"\b(Software Engineer|Software Developer|Senior Engineer|Lead Engineer|Principal Engineer|Architect|Cloud Architect|Platform Architect|CTO|Technical Director|Tech Lead|Project Manager|Technical Lead|Head of Architecture)\b", jobTitles) },
        { "Skills", (@"\b(Cloud Architecture|Microservices|API Design|System Design|Database Design|Scalability|Performance Optimization|Security|ETL|ELT|Data Processing|Data Migration|Mentorship|Technical Leadership|Serverless|Cloud Migration)\b", skills) },
        { "Education", (@"\b(Bachelor|Master|PhD|MSc|BSc|MBA|Doctorate)\s(of|in)\s(Science|Arts|Engineering|Computer Science|Business|Information Technology)\b", education) }
    };
    
    // Apply regex patterns for more focused entity extraction
    foreach (var (_, (pattern, category)) in patterns)
    {
        foreach (Match match in Regex.Matches(sanitizedMarkdown, pattern, RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                // Normalize the case to avoid duplicates like "microservices" and "Microservices"
                category.Add(NormalizeEntityText(match.Value.Trim()));
            }
        }
    }
    
    // Extract additional key terms from bullet points and job descriptions
    ExtractKeyTermsFromBulletPoints(sanitizedMarkdown, skills, technologies);
    
    // Create a clean, categorized summary for ATS
    var summary = new StringBuilder();
    
    if (technologies.Count > 0)
        summary.AppendLine($"**Technologies:** {string.Join(", ", technologies.OrderBy(t => t))}");
    
    if (skills.Count > 0)
        summary.AppendLine($"**Skills:** {string.Join(", ", skills.OrderBy(s => s))}");
    
    if (jobTitles.Count > 0)
        summary.AppendLine($"**Roles:** {string.Join(", ", jobTitles.OrderBy(t => t))}");
    
    if (education.Count > 0)
        summary.AppendLine($"**Education:** {string.Join(", ", education.OrderBy(e => e))}");
    
    // Only highlight important keywords in the original markdown
    // and avoid redundant highlighting
    string enhancedMarkdown = markdown;
    
    // Collect all keywords for targeted highlighting (only highlight them once)
    var keywordsToHighlight = new HashSet<string>();
    
    // Add only specific technologies and skills for highlighting
    // to avoid over-highlighting the document
    foreach (var tech in technologies.Where(t => t.Length > 3))
        keywordsToHighlight.Add(tech);
    
    foreach (var skill in skills.Where(s => s.Length > 5))
        keywordsToHighlight.Add(skill);
    
    // Track words already highlighted to prevent excessive highlighting
    var highlightedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    // Apply highlighting for each keyword, but only if not already highlighted
    foreach (var keyword in keywordsToHighlight.OrderByDescending(k => k.Length))
    {
        if (highlightedWords.Contains(keyword))
            continue;
            
        enhancedMarkdown = Regex.Replace(
            enhancedMarkdown,
            $@"\b{Regex.Escape(keyword)}\b",
            match => {
                // Skip if already in markdown formatting or part of a URL/email
                if (match.Value.Contains("**") || 
                    match.Value.Contains("__") || 
                    IsPartOfFormattedText(enhancedMarkdown, match.Index))
                    return match.Value;
                
                highlightedWords.Add(match.Value);
                return $"**{match.Value}**";
            },
            RegexOptions.IgnoreCase
        );
    }
    
    // Only prepend if we have entities to show
    if (summary.Length > 0)
    {
        enhancedMarkdown = summary.ToString() + "\n\n" + enhancedMarkdown;
    }
    
    return enhancedMarkdown;
}

private static string SanitizeMarkdown(string markdown)
{
    // Remove existing markdown formatting to avoid interference
    string sanitized = Regex.Replace(markdown, @"\*\*(.*?)\*\*", "$1");
    sanitized = Regex.Replace(sanitized, @"__(.*?)__", "$1");
    sanitized = Regex.Replace(sanitized, @"\*(.*?)\*", "$1");
    sanitized = Regex.Replace(sanitized, @"_(.*?)_", "$1");
    
    // Remove URLs and email addresses to avoid detecting them as entities
    sanitized = Regex.Replace(sanitized, @"https?://\S+", "");
    sanitized = Regex.Replace(sanitized, @"\b[\w\.-]+@[\w\.-]+\.\w+\b", "");
    
    return sanitized;
}

private static string NormalizeEntityText(string text)
{
    // Capitalize the first letter of each word for consistency
    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
}

private static void ExtractKeyTermsFromBulletPoints(string markdown, HashSet<string> skills, HashSet<string> technologies)
{
    // Find bullet points which often contain key skills/technologies
    var bulletPoints = Regex.Matches(markdown, @"[-•*]\s*(.*?)(?=\n|$)");
    
    // Common technical terms that might appear in bullet points
    var technicalTerms = new string[] {
        "Cloud", "AWS", "Azure", "GCP", "Kubernetes", "Docker", "CI/CD", 
        "Microservices", "Serverless", "API", "REST", "GraphQL", "ETL", "ELT",
        "Data Engineering", "Data Migration", "Cloud Architecture", "System Design"
    };
    
    foreach (Match bulletPoint in bulletPoints)
    {
        string content = bulletPoint.Groups[1].Value;
        
        // Check for technical terms in bullet points
        foreach (var term in technicalTerms)
        {
            if (Regex.IsMatch(content, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            {
                // Determine if it's more likely a skill or technology
                if (term.Contains("Architecture") || term.Contains("Design") || 
                    term.Contains("Engineering") || term.Contains("Migration"))
                {
                    skills.Add(NormalizeEntityText(term));
                }
                else
                {
                    technologies.Add(NormalizeEntityText(term));
                }
            }
        }
    }
}

private static bool IsPartOfFormattedText(string markdown, int position)
{
    // Check if this position is inside an existing markdown format
    // or part of a URL/email/code block
    int startCheck = Math.Max(0, position - 50);
    int endCheck = Math.Min(markdown.Length, position + 50);
    
    string context = markdown.Substring(startCheck, endCheck - startCheck);
    
    // Check for common formatting patterns
    return context.Contains("](") || // URL part
           context.Contains("```") || // code block
           context.Contains("`") || // inline code
           context.Contains("@"); // likely email
}
    private class TextData
    {
        public string Text { get; set; }
    }
    
    private class TokenizedText
    {
        public string[] Tokens { get; set; }
    }
}