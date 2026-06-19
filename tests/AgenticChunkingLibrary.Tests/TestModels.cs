using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgenticChunkingLibrary.Tests
{
    // Root JSON has "testCases" at the top level alongside a "testSuite" metadata block.
    // Deserialising to this record picks up the array and ignores the metadata.
    public record TestSuite(
        [property: JsonPropertyName("testCases")]
        List<TestCase> TestCases
    );

    public record TestCase(
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("name")]
        string Name,
        [property: JsonPropertyName("category")]
        string Category,
        [property: JsonPropertyName("description")]
        string Description,
        [property: JsonPropertyName("input")]
        TestInput Input,
        [property: JsonPropertyName("expectedExtractionOutput")]
        ExpectedExtractionOutput ExpectedExtractionOutput,
        [property: JsonPropertyName("expectedGroupingOutput")]
        ExpectedGroupingOutput ExpectedGroupingOutput,
        [property: JsonPropertyName("passCriteria")]
        string PassCriteria
    );

    public record TestInput(
        // Nullable: integration test cases (TC-031+) use sourceFile/sourceDescription instead
        [property: JsonPropertyName("chunks")]
        List<TestChunk>? Chunks
    );

    public record TestChunk(
        [property: JsonPropertyName("text")]
        string Text,
        [property: JsonPropertyName("metadata")]
        TestChunkMetadata Metadata
    );

    public record TestChunkMetadata(
        [property: JsonPropertyName("chunkId")]
        string ChunkId,
        [property: JsonPropertyName("tokenEstimate")]
        int TokenEstimate,
        [property: JsonPropertyName("sentenceCount")]
        int SentenceCount
    );

    public record ExpectedExtractionOutput(
        [property: JsonPropertyName("propositionCount")]
        int PropositionCount,
        // Nullable: some cases use propositionCountRange + note instead
        [property: JsonPropertyName("propositions")]
        List<string>? Propositions,
        [property: JsonPropertyName("note")]
        string? Note
    );

    public record ExpectedGroupingOutput(
        [property: JsonPropertyName("chunkCount")]
        int ChunkCount,
        // Nullable: TC-035 uses expectedThemes + chunkCountRange instead
        [property: JsonPropertyName("themes")]
        List<string>? Themes
    );
}
