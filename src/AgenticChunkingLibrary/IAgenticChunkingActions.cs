using System.Collections.Generic;
using AgenticChunkingLibrary.Models;
using OutSystems.ExternalLibraries.SDK;

namespace AgenticChunkingLibrary
{
    [OSInterface(
        Name = "AgenticChunkingActions",
        Description = "Stateless text pre-processing and output normalisation for Level 5 Agentic Chunking workflows in ODC.")]
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
            Description = "Parses the raw JSON response from the thematic grouping LLM call and maps it to ODC-compliant AgenticChunk structs. Call this after the grouping AI Gateway call completes.")]
        AgenticResponse NormaliseAgenticOutput(
            [OSParameter(Description = "The raw JSON string returned by the grouping LLM. Expected format: [{category: string, facts: string[]}]")]
            string rawGroupingJson,
            [OSParameter(Description = "The document identifier to embed in each ChunkId. Example: DOC-001")]
            string documentId);
    }
}
