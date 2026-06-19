using System;
using System.Collections.Generic;
using AgenticChunkingLibrary;
using Xunit;

namespace AgenticChunkingLibrary.Tests
{
    public class NormaliseAgenticOutputTests
    {
        private readonly AgenticChunkingActions _sut = new();

        private const string TwoCategories = """
            [
              {"category": "Finance", "facts": ["Revenue grew 10%.", "Costs reduced by 5%."]},
              {"category": "Operations", "facts": ["Headcount stable.", "New office opened."]}
            ]
            """;

        [Fact]
        public void ValidJson_IsSuccessTrue()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Empty(result.ErrorDetail);
        }

        [Fact]
        public void ValidJson_ChunkIdFormat()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.Equal("DOC-001-0001", result.Chunks[0].ChunkId);
            Assert.Equal("DOC-001-0002", result.Chunks[1].ChunkId);
        }

        [Fact]
        public void ValidJson_MergedContentIsFactsJoinedBySpace()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.Equal("Revenue grew 10%. Costs reduced by 5%.", result.Chunks[0].MergedContent);
        }

        [Fact]
        public void ValidJson_PropositionCountMatchesFactsLength()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.Equal(2, result.Chunks[0].PropositionCount);
            Assert.Equal(2, result.Chunks[1].PropositionCount);
        }

        [Fact]
        public void ValidJson_CharacterCountMatchesMergedContentLength()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            foreach (var chunk in result.Chunks)
                Assert.Equal(chunk.MergedContent.Length, chunk.CharacterCount);
        }

        [Fact]
        public void ValidJson_TokenEstimateIsCharCountDividedBy4()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            foreach (var chunk in result.Chunks)
                Assert.Equal(chunk.CharacterCount / 4, chunk.TokenEstimate);
        }

        [Fact]
        public void ValidJson_HashStartsWithSha256Prefix()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            foreach (var chunk in result.Chunks)
                Assert.StartsWith("sha256-", chunk.Hash);
        }

        [Fact]
        public void ValidJson_TotalChunksMatchesCategoryCount()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.Equal(2, result.TotalChunks);
            Assert.Equal(2, result.Chunks.Count);
        }

        [Fact]
        public void ValidJson_TotalPropositionsIsSumOfAllFacts()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "DOC-001");
            Assert.Equal(4, result.TotalPropositions);
        }

        [Fact]
        public void EmptyRawJson_IsSuccessFalse_ErrorDetailPopulated()
        {
            var result = _sut.NormaliseAgenticOutput("", "DOC-001");
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.ErrorDetail);
        }

        [Fact]
        public void MalformedJson_IsSuccessFalse_ErrorDetailPopulated()
        {
            var result = _sut.NormaliseAgenticOutput("not json at all {{", "DOC-001");
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.ErrorDetail);
        }

        [Fact]
        public void MissingCategoryField_SkipsThatObject_ProcessesRest()
        {
            string json = """
                [
                  {"facts": ["Orphaned fact."]},
                  {"category": "Valid", "facts": ["Good fact."]}
                ]
                """;
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Single(result.Chunks);
            Assert.Equal("Valid", result.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void MissingFactsField_SkipsThatObject()
        {
            string json = """
                [
                  {"category": "NoFacts"},
                  {"category": "HasFacts", "facts": ["A fact."]}
                ]
                """;
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Single(result.Chunks);
            Assert.Equal("HasFacts", result.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void EmptyFactsArray_SkipsThatObject()
        {
            string json = """
                [
                  {"category": "Empty", "facts": []},
                  {"category": "NotEmpty", "facts": ["Has content."]}
                ]
                """;
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Single(result.Chunks);
            Assert.Equal("NotEmpty", result.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void EmptyDocumentId_DefaultsToDocUnknown()
        {
            var result = _sut.NormaliseAgenticOutput(TwoCategories, "");
            Assert.True(result.IsSuccess);
            Assert.StartsWith("DOC-UNKNOWN-", result.Chunks[0].ChunkId);
            Assert.Equal("DOC-UNKNOWN", result.Chunks[0].DocumentId);
        }

        [Fact]
        public void MarkdownCodeFences_AreStrippedBeforeParsing()
        {
            string json = "```json\n[\n  {\"category\": \"Tech\", \"facts\": [\"Fact one.\"]}\n]\n```";
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Single(result.Chunks);
        }

        [Fact]
        public void SingleCategoryOneFact_OneChunkWithPropositionCountOne()
        {
            string json = """[{"category": "Solo", "facts": ["Only fact."]}]""";
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.True(result.IsSuccess);
            Assert.Single(result.Chunks);
            Assert.Equal(1, result.Chunks[0].PropositionCount);
        }

        [Fact]
        public void FactsJoinedWithSingleSpaceSeparator()
        {
            string json = """[{"category": "Spacing", "facts": ["First.", "Second."]}]""";
            var result = _sut.NormaliseAgenticOutput(json, "DOC-001");
            Assert.Equal("First. Second.", result.Chunks[0].MergedContent);
        }
    }
}
