using System.Collections.Generic;
using AgenticChunkingLibrary.Models;
using OutSystems.ExternalLibraries.SDK;

namespace AgenticChunkingLibrary
{
    [OSInterface(
        Name = "AgenticChunkingHelpers",
        Description = "Stateless text pre-processing and output normalisation for Level 5 Agentic Chunking workflows in ODC.",
        IconResourceName = "AgenticChunkingLibrary.AgenticChunk.png")]
    public interface IAgenticChunkingActions
    {
        [OSAction(
            Description = "Splits input text into token-safe batches for the extraction prompt. Call this before the first AI Gateway call. Returns a list of batch strings each within the maxTokensPerBatch limit.")]
        List<string> PreChunkForExtraction(
            [OSParameter(Description = "The full source text to batch. Typically the concatenated text of all Level 4 semantic chunks.")]
            string sourceText,
            [OSParameter(Description = "Maximum tokens per batch. Use 2000 for safety with most models. One token approximates four characters.")]
            int maxTokensPerBatch);

        [OSAction(
            Description = "Parses the raw JSON response from an extraction AI Gateway call into a clean list of proposition strings. Handles markdown code fences, escaped inner quotes, and malformed JSON safely. Call this after each extraction batch call and accumulate the results. Returns an empty list if the response cannot be parsed.")]
        List<string> ParsePropositions(
            [OSParameter(Description = "The raw string returned by the extraction AI Gateway call. Expected format: a JSON array of strings. Markdown code fences are stripped automatically.")]
            string rawExtractionJson);

        [OSAction(
            Description = "Parses the raw JSON response from the thematic grouping AI Gateway call and maps it to ODC-compliant AgenticChunk structs. Call this after the grouping AI Gateway call completes.")]
        AgenticResponse NormaliseAgenticOutput(
            [OSParameter(Description = "The raw JSON string returned by the grouping AI Gateway call. Expected format: [{category: string, facts: string[]}]")]
            string rawGroupingJson,
            [OSParameter(Description = "The document identifier to embed in each ChunkId. Example: DOC-001")]
            string documentId);
    }
}
