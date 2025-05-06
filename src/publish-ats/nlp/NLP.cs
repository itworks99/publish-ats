using System.Text.RegularExpressions;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using java.util;

namespace publish_ats.nlp;

/// <summary>
/// Provides Natural Language Processing (NLP) utilities for optimizing markdown content for ATS (Applicant Tracking Systems).
/// </summary>
internal static class Nlp
{
    /// <summary>
    /// Optimizes the given markdown content for ATS by detecting named entities (e.g., organizations, people, locations)
    /// and highlighting them in bold. Additionally, a summary of detected entities is prepended to the markdown.
    /// </summary>
    /// <param name="markdown">The markdown content to be optimized.</param>
    /// <returns>The optimized markdown content with detected entities highlighted and summarized.</returns>
    internal static string OptimizeForAts(string markdown)
    {
        // Set up properties for the Stanford NLP pipeline
        var props = new Properties();
        props.setProperty("annotators", "tokenize,ssplit,pos,lemma,ner");

        // Initialize the Stanford NLP pipeline with the specified properties
        var pipeline = new StanfordCoreNLP(props);

        // Create an annotation object for the input markdown
        var document = new Annotation(markdown);

        // Annotate the document using the NLP pipeline
        pipeline.annotate(document);

        // Extract sentences from the annotated document
        var sentences = document.get(new CoreAnnotations.SentencesAnnotation().getClass()) as ArrayList;
        var entities = new HashSet<string>(); // Store detected named entities

        // Process each sentence in the document
        if (sentences != null)
            foreach (Annotation sentence in sentences)
            {
                // Extract tokens (words) from the sentence
                var tokens = sentence.get(new CoreAnnotations.TokensAnnotation().getClass()) as ArrayList;
                if (tokens != null)
                    foreach (CoreLabel token in tokens)
                    {
                        // Get the word and its named entity recognition (NER) tag
                        var word = token.get(new CoreAnnotations.TextAnnotation().getClass()).ToString();
                        var ner = token.get(new CoreAnnotations.NamedEntityTagAnnotation().getClass()).ToString();

                        // Add the word to the entity set if it matches specific NER tags
                        if (ner is "ORGANIZATION" or "PERSON" or "LOCATION" or "MISC" && word != null)
                        {
                            entities.Add(word);
                        }
                    }
            }

        // Highlight detected entities in the markdown content
        foreach (var entity in entities)
        {
            markdown = Regex.Replace(markdown, $@"\b{Regex.Escape(entity)}\b", "**" + entity + "**",
                RegexOptions.IgnoreCase);
        }

        // Prepend a summary of detected entities to the markdown content
        if (entities.Count > 0)
        {
            var summary = string.Join(", ", entities.OrderBy(e => e));
            markdown = $"**Detected Entities:** {summary}\n\n" + markdown;
        }

        return markdown; // Return the optimized markdown content
    }
}