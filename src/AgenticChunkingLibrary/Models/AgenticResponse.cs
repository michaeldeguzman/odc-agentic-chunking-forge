using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace AgenticChunkingLibrary.Models
{
    [OSStructure(Description = "The normalised output of an agentic chunking grouping pass.")]
    public struct AgenticResponse
    {
        [OSStructureField(Description = "The list of thematic chunks produced")]
        public List<AgenticChunk> Chunks { get; set; }

        [OSStructureField(Description = "Total number of chunks produced")]
        public int TotalChunks { get; set; }

        [OSStructureField(Description = "Total number of propositions across all chunks")]
        public int TotalPropositions { get; set; }

        [OSStructureField(Description = "Total estimated token count across all chunks")]
        public int TotalTokenEstimate { get; set; }

        [OSStructureField(Description = "True if parsing succeeded, false if the response could not be parsed")]
        public bool IsSuccess { get; set; }

        [OSStructureField(Description = "Error detail if IsSuccess is false, empty string otherwise")]
        public string ErrorDetail { get; set; }
    }
}
