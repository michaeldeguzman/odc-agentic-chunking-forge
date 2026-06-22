# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-22

### Added

- `PreChunkForExtraction` action — splits source text into token-safe batches for the extraction prompt
- `ParsePropositions` action — parses the extraction LLM JSON response into a clean list of proposition strings; handles markdown fences, doubled quotes, and outer-quoted responses
- `NormaliseAgenticOutput` action — parses the grouping LLM JSON response into `AgenticChunk` structs
- `AgenticChunk` structure with `ChunkId`, `DocumentId`, `ThematicCategory`, `Propositions`, `MergedContent`, `PropositionCount`, `CharacterCount`, `TokenEstimate`, `Hash`, and `IsTag` fields
- `AgenticResponse` wrapper structure with `Chunks`, `TotalChunks`, `TotalPropositions`, `TotalTokenEstimate`, `IsSuccess`, and `ErrorDetail` fields
- 89 unit tests covering batching logic, JSON parsing, fence stripping, and failure modes
