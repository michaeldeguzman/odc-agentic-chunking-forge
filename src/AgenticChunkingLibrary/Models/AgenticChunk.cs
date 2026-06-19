using OutSystems.ExternalLibraries.SDK;

namespace AgenticChunkingLibrary.Models
{
    [OSStructure(Description = "A single thematic chunk produced by agentic grouping.")]
    public struct AgenticChunk
    {
        [OSStructureField(Description = "Unique identifier for this chunk")]
        public string ChunkId { get; set; }

        [OSStructureField(Description = "The document identifier passed by the caller")]
        public string DocumentId { get; set; }

        [OSStructureField(Description = "The thematic category label assigned by the LLM")]
        public string ThematicCategory { get; set; }

        [OSStructureField(Description = "The merged proposition text for this chunk")]
        public string MergedContent { get; set; }

        [OSStructureField(Description = "Number of propositions in this chunk")]
        public int PropositionCount { get; set; }

        [OSStructureField(Description = "Character count of MergedContent")]
        public int CharacterCount { get; set; }

        [OSStructureField(Description = "Approximate token count using chars divided by 4")]
        public int TokenEstimate { get; set; }

        [OSStructureField(Description = "SHA256 hash of MergedContent for audit purposes")]
        public string Hash { get; set; }
    }
}
