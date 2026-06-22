# ODC Agentic Chunking Library

[![CI](https://github.com/michaeldeguzman/odc-agentic-chunking/actions/workflows/ci.yml/badge.svg)](https://github.com/michaeldeguzman/odc-agentic-chunking/actions/workflows/ci.yml)

An OutSystems ODC External Logic library that provides stateless text pre-processing and output normalisation for Level 5 Agentic Chunking workflows. It handles the C# work that sits on either side of your AI Gateway calls — splitting input text into safe batches, parsing proposition lists, and normalising grouped output into typed ODC structures.

> **Platform:** OutSystems Developer Cloud (ODC) only. Not compatible with OutSystems 11.

## Actions

### `PreChunkForExtraction`

Splits a large source text into token-safe batches before the first AI Gateway call. Paragraph boundaries are respected; oversized paragraphs are split at sentence boundaries.

| Parameter | Type | Description |
|---|---|---|
| `sourceText` | Text | The full source text to batch |
| `maxTokensPerBatch` | Integer | Maximum tokens per batch (2000 is a safe default; 1 token ≈ 4 characters) |

**Returns** `List<Text>` — ordered list of batch strings, each within the token limit.

---

### `ParsePropositions`

Parses the raw JSON response from an extraction AI Gateway call into a clean list of proposition strings. Handles markdown code fences, escaped inner quotes, and outer-quoted responses.

| Parameter | Type | Description |
|---|---|---|
| `rawExtractionJson` | Text | The raw string returned by the extraction AI Gateway |

**Returns** `List<Text>` — list of proposition strings. Returns an empty list if the response cannot be parsed.

**Expected AI Gateway response format:**
```json
["Proposition one.", "Proposition two.", "Proposition three."]
```

---

### `NormaliseAgenticOutput`

Parses the raw JSON response from the thematic grouping AI Gateway call and maps it to typed `AgenticChunk` structures.

| Parameter | Type | Description |
|---|---|---|
| `rawGroupingJson` | Text | The raw JSON string returned by the grouping AI Gateway |
| `documentId` | Text | Document identifier embedded in each `ChunkId` (e.g. `DOC-001`) |

**Returns** `AgenticResponse` — see structures below.

**Expected AI Gateway response format:**
```json
[
  { "category": "Finance", "facts": ["Revenue grew 10%.", "Costs reduced by 5%."] },
  { "category": "Operations", "facts": ["Headcount stable.", "New office opened."] }
]
```

---

## Structures

### `AgenticChunk`

A single thematic chunk produced by the grouping pass.

| Field | Type | Description |
|---|---|---|
| `ChunkId` | Text | Unique identifier — `{DocumentId}-{sequence}` (e.g. `DOC-001-0001`) |
| `DocumentId` | Text | The document identifier passed by the caller |
| `ThematicCategory` | Text | The thematic category label assigned by the AI Gateway |
| `Propositions` | List&lt;Text&gt; | The individual propositions that make up this chunk |
| `MergedContent` | Text | All propositions joined by a single space |
| `PropositionCount` | Integer | Number of propositions in this chunk |
| `CharacterCount` | Integer | Character count of `MergedContent` |
| `TokenEstimate` | Integer | Approximate token count (`CharacterCount / 4`) |
| `Hash` | Text | SHA-256 hash of `MergedContent` prefixed with `sha256-` |
| `IsTag` | Boolean | True if this chunk represents a tag rather than a thematic content group |

### `AgenticResponse`

The normalised output returned by `NormaliseAgenticOutput`.

| Field | Type | Description |
|---|---|---|
| `Chunks` | List&lt;AgenticChunk&gt; | The thematic chunks produced |
| `TotalChunks` | Integer | Total number of chunks |
| `TotalPropositions` | Integer | Total number of propositions across all chunks |
| `TotalTokenEstimate` | Integer | Total estimated token count across all chunks |
| `IsSuccess` | Boolean | True if parsing succeeded |
| `ErrorDetail` | Text | Error description when `IsSuccess` is false, empty otherwise |

---

## Installation

Download the latest `AgenticChunkingLibrary.zip` from the [Releases](https://github.com/michaeldeguzman/odc-agentic-chunking/releases) page, then upload it to your ODC tenant:

1. Open **ODC Portal**
2. Go to **External Logic**
3. Click **Upload** and select `AgenticChunkingLibrary.zip`
4. Wait for validation and publishing to complete

The library exposes the `AgenticChunkingHelpers` interface. Reference it in your ODC app to access the three actions.

---

## Building from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8).

```bash
# Run all tests (89 total)
dotnet test

# Build release
dotnet publish src/AgenticChunkingLibrary -c Release -o publish/AgenticChunkingLibrary

# Package for ODC upload (DLLs must be at zip root)
cd publish/AgenticChunkingLibrary
zip -j ../../AgenticChunkingLibrary.zip \
  AgenticChunkingLibrary.dll \
  AgenticChunkingLibrary.deps.json \
  AgenticChunkingLibrary.pdb \
  OutSystems.ExternalLibraries.SDK.dll
```

Upload `AgenticChunkingLibrary.zip` to ODC Portal → External Logic.

---

## What this library does NOT do

- Call the AI Gateway or any external service
- Store state between calls
- Validate business rules (min/max chunk size, document policies) — those belong in ODC

---

## License

[MIT](LICENSE) © 2025 Michael De Guzman
