using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using AgenticChunkingLibrary;
using AgenticChunkingLibrary.Models;
using Xunit;

namespace AgenticChunkingLibrary.Tests
{
    public class AgenticTestCasesTests : IClassFixture<TestSuiteFixture>
    {
        private readonly TestSuiteFixture _fixture;
        private readonly AgenticChunkingActions _actions;

        public AgenticTestCasesTests(TestSuiteFixture fixture)
        {
            _fixture = fixture;
            _actions = new AgenticChunkingActions();
        }

        // ============================================================
        // PRECHUNKFOREXTRACTION TESTS
        // These tests verify batching behaviour only.
        // They do not test proposition quality — that is an LLM concern.
        // ============================================================

        [Fact]
        public void TC021_SingleChunkSingleSentence_ReturnsOneBatch()
        {
            var tc = _fixture.GetCase("TC-021");
            string sourceText = BuildSourceText(tc);

            var batches = _actions.PreChunkForExtraction(sourceText, 2000);

            Assert.True(batches.Count == 1,
                $"{tc.Id} - {tc.Name}: Expected 1 batch, got {batches.Count}");
            Assert.Contains(
                "floor area ratio",
                batches[0],
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TC022_SingleChunkTenSentences_FitsInOneBatch()
        {
            var tc = _fixture.GetCase("TC-022");
            string sourceText = BuildSourceText(tc);

            // tokenEstimate is 188, well under 2000 limit
            var batches = _actions.PreChunkForExtraction(sourceText, 2000);

            Assert.True(batches.Count == 1,
                $"{tc.Id} - {tc.Name}: Expected 1 batch, got {batches.Count}");
        }

        [Fact]
        public void TC026_VeryLongChunk_BatchesCorrectly()
        {
            var tc = _fixture.GetCase("TC-026");
            string sourceText = BuildSourceText(tc);

            // tokenEstimate is 312; use a tight limit of 100 to force batching
            var batches = _actions.PreChunkForExtraction(sourceText, 100);

            Assert.True(batches.Count > 1,
                $"{tc.Id} - {tc.Name}: Expected multiple batches with limit=100, got {batches.Count}");

            foreach (var batch in batches)
            {
                int tokens = batch.Length / 4;
                Assert.True(tokens <= 100,
                    $"{tc.Id}: Batch exceeded token limit. Tokens: {tokens}");
            }
        }

        [Fact]
        public void TC025_EmptyChunkInInput_SkipsEmptyReturnsValidBatches()
        {
            var tc = _fixture.GetCase("TC-025");
            string sourceText = BuildSourceText(tc);

            var batches = _actions.PreChunkForExtraction(sourceText, 2000);

            Assert.True(batches.Count >= 1,
                $"{tc.Id} - {tc.Name}: Expected at least 1 batch from valid chunks");

            foreach (var batch in batches)
            {
                Assert.False(string.IsNullOrWhiteSpace(batch),
                    $"{tc.Id}: A batch was empty or whitespace");
            }
        }

        [Fact]
        public void PreChunk_EmptyString_ReturnsEmptyList()
        {
            var batches = _actions.PreChunkForExtraction(string.Empty, 2000);
            Assert.Empty(batches);
        }

        [Fact]
        public void PreChunk_NullInput_ReturnsEmptyList()
        {
            var batches = _actions.PreChunkForExtraction(null!, 2000);
            Assert.Empty(batches);
        }

        [Fact]
        public void PreChunk_ZeroTokenLimit_DefaultsTo2000()
        {
            var batches = _actions.PreChunkForExtraction(
                "Lithium-ion batteries store energy through ion movement.", 0);
            Assert.True(batches.Count >= 1);
        }

        [Fact]
        public void PreChunk_MultipleChunks_AllChunksSurviveInBatches()
        {
            // TC-013 has 4 concrete Battery Chemistry chunks
            var tc = _fixture.GetCase("TC-013");
            string sourceText = BuildSourceText(tc);

            var batches = _actions.PreChunkForExtraction(sourceText, 2000);

            string combined = string.Join(" ", batches);
            Assert.Contains("lithium", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cathode", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("solid-state", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("energy density", combined, StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // PARSEPROPOSITIONS TESTS
        // These tests verify that raw LLM extraction responses are
        // cleaned and parsed correctly before accumulation.
        // ============================================================

        [Fact]
        public void ParsePropositions_ValidJsonArray_ReturnsCorrectList()
        {
            string json = """["Kubernetes is a container orchestration platform.", "Kubernetes automates deployment of containerised applications."]""";

            var result = _actions.ParsePropositions(json);

            Assert.Equal(2, result.Count);
            Assert.Equal("Kubernetes is a container orchestration platform.", result[0]);
        }

        [Fact]
        public void ParsePropositions_EmptyString_ReturnsEmptyList()
        {
            var result = _actions.ParsePropositions(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void ParsePropositions_NullInput_ReturnsEmptyList()
        {
            var result = _actions.ParsePropositions(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void ParsePropositions_MalformedJson_ReturnsEmptyListWithoutThrowing()
        {
            var result = _actions.ParsePropositions("this is not json");
            Assert.Empty(result);
        }

        [Fact]
        public void ParsePropositions_MarkdownJsonFences_StrippedBeforeParsing()
        {
            string raw = "```json\n" +
                         "[\"Kubernetes is a container orchestration platform.\", " +
                         "\"Kubernetes automates deployment of containerised applications.\"]\n" +
                         "```";

            var result = _actions.ParsePropositions(raw);

            Assert.Equal(2, result.Count);
            Assert.Equal("Kubernetes is a container orchestration platform.", result[0]);
        }

        [Fact]
        public void ParsePropositions_MarkdownFencesWithoutLanguageTag_StrippedBeforeParsing()
        {
            string raw = "```\n[\"Kubernetes is a platform.\"]\n```";

            var result = _actions.ParsePropositions(raw);

            Assert.Single(result);
            Assert.Equal("Kubernetes is a platform.", result[0]);
        }

        [Fact]
        public void ParsePropositions_DoubledInnerQuotes_NormalisedToSingleQuotes()
        {
            string raw = """
                [
                  ""Kubernetes is a container orchestration platform."",
                  ""Kubernetes automates the deployment of containerised applications.""
                ]
                """;

            var result = _actions.ParsePropositions(raw);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, p => p.StartsWith("\"") || p.EndsWith("\""));
        }

        [Fact]
        public void ParsePropositions_FourItemArray_ReturnsAllFour()
        {
            string raw = "```json\n" +
                         "[\n" +
                         "  \"\"Kubernetes is a container orchestration platform.\"\",\n" +
                         "  \"\"Kubernetes automates the deployment of containerised applications across a cluster of machines.\"\",\n" +
                         "  \"\"Kubernetes automates the scaling of containerised applications across a cluster of machines.\"\",\n" +
                         "  \"\"Kubernetes automates the management of containerised applications across a cluster of machines.\"\"\n" +
                         "]\n" +
                         "```";

            var result = _actions.ParsePropositions(raw);

            Assert.Equal(4, result.Count);
            Assert.All(result, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p));
                Assert.DoesNotContain("```", p);
            });
        }

        [Fact]
        public void ParsePropositions_JsonObjectNotArray_ReturnsEmptyList()
        {
            string raw = """{"category": "Cloud", "facts": ["Fact one."]}""";
            var result = _actions.ParsePropositions(raw);
            Assert.Empty(result);
        }

        // ============================================================
        // NORMALISEAGENTICOUTPUT TESTS
        // These tests verify JSON parsing and struct mapping.
        // Synthetic grouping responses are built from expectedGroupingOutput
        // so failures are traceable to the relevant test case in the JSON.
        // ============================================================

        [Fact]
        public void TC001_SingleChunkSingleDomain_ParsesCorrectly()
        {
            var tc = _fixture.GetCase("TC-001");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-001");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false. ErrorDetail: {response.ErrorDetail}");
            Assert.Equal(tc.ExpectedGroupingOutput.ChunkCount, response.TotalChunks);
            Assert.Equal(tc.ExpectedGroupingOutput.Themes![0], response.Chunks[0].ThematicCategory);
            Assert.Equal("TC-001", response.Chunks[0].DocumentId);
            Assert.Equal("TC-001-0001", response.Chunks[0].ChunkId);
            Assert.Equal(1, response.Chunks[0].PropositionCount);
            Assert.False(string.IsNullOrWhiteSpace(response.Chunks[0].MergedContent));
        }

        [Fact]
        public void TC011_TwoChunksSameDomain_ConsolidatestoOneChunk()
        {
            var tc = _fixture.GetCase("TC-011");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-011");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Battery Chemistry", response.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void TC012_ThreeChunksSameDomain_ConsolidatesToOneChunk()
        {
            var tc = _fixture.GetCase("TC-012");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-012");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Cloud Infrastructure", response.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void TC013_FourChunksBatteryChemistry_ConsolidatesToOneChunk()
        {
            var tc = _fixture.GetCase("TC-013");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-013");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Battery Chemistry", response.Chunks[0].ThematicCategory);
            Assert.True(response.Chunks[0].PropositionCount > 0);
        }

        [Fact]
        public void TC014_MixedDomainChunk_ProducesTwoChunks()
        {
            var tc = _fixture.GetCase("TC-014");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-014");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(2, response.TotalChunks);

            var themes = response.Chunks.Select(c => c.ThematicCategory).ToList();
            Assert.Contains("Human Metabolism", themes);
            Assert.Contains("Genomics", themes);
        }

        [Fact]
        public void TC015_ThreeCleanChunks_ProducesThreeChunks()
        {
            var tc = _fixture.GetCase("TC-015");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-015");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(3, response.TotalChunks);
        }

        [Fact]
        public void TC016_AdjacentDifferentDomains_ProducesTwoChunks()
        {
            var tc = _fixture.GetCase("TC-016");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-016");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(2, response.TotalChunks);

            var themes = response.Chunks.Select(c => c.ThematicCategory).ToList();
            Assert.Contains("Italian Renaissance", themes);
            Assert.Contains("Urban Planning", themes);
        }

        [Fact]
        public void TC023_AllChunksSameDomain_ProducesOneChunk()
        {
            var tc = _fixture.GetCase("TC-023");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-023");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Urban Planning", response.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void TC024_SixDifferentDomains_ProducesSixChunks()
        {
            var tc = _fixture.GetCase("TC-024");
            string json = BuildSyntheticGroupingJson(tc);

            var response = _actions.NormaliseAgenticOutput(json, "TC-024");

            Assert.True(response.IsSuccess, $"{tc.Id}: IsSuccess was false");
            Assert.Equal(6, response.TotalChunks);
        }

        // ============================================================
        // NORMALISAGENTICOUTPUT - FIELD VALUE TESTS
        // ============================================================

        [Fact]
        public void Normalise_ChunkId_FollowsDocumentIdSequenceFormat()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Battery Chemistry", new[] { "Fact one.", "Fact two." }),
                ("Cloud Infrastructure", new[] { "Fact three." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "MYDOC");

            Assert.Equal("MYDOC-0001", response.Chunks[0].ChunkId);
            Assert.Equal("MYDOC-0002", response.Chunks[1].ChunkId);
        }

        [Fact]
        public void Normalise_MergedContent_IsPropositionsJoinedBySpace()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Battery Chemistry", new[] { "Fact one.", "Fact two." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.Equal("Fact one. Fact two.", response.Chunks[0].MergedContent);
        }

        [Fact]
        public void Normalise_CharacterCount_MatchesMergedContentLength()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Genomics", new[] { "CRISPR-Cas9 is a gene editing tool." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.Equal(
                response.Chunks[0].MergedContent.Length,
                response.Chunks[0].CharacterCount);
        }

        [Fact]
        public void Normalise_TokenEstimate_IsCharacterCountDividedByFour()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Genomics", new[] { "CRISPR-Cas9 is a gene editing tool." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            int expected = response.Chunks[0].CharacterCount / 4;
            Assert.Equal(expected, response.Chunks[0].TokenEstimate);
        }

        [Fact]
        public void Normalise_Hash_StartsWithSha256Prefix()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Genomics", new[] { "CRISPR-Cas9 is a gene editing tool." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.StartsWith("sha256-", response.Chunks[0].Hash);
        }

        [Fact]
        public void Normalise_TotalPropositions_IsSumOfAllFactsCounts()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Battery Chemistry", new[] { "Fact one.", "Fact two.", "Fact three." }),
                ("Genomics", new[] { "Fact four.", "Fact five." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.Equal(5, response.TotalPropositions);
        }

        [Fact]
        public void Normalise_TotalTokenEstimate_IsSumOfChunkTokenEstimates()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Battery Chemistry", new[] { "Fact one.", "Fact two." }),
                ("Genomics", new[] { "Fact three." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            int expectedTotal = response.Chunks.Sum(c => c.TokenEstimate);
            Assert.Equal(expectedTotal, response.TotalTokenEstimate);
        }

        // ============================================================
        // NORMALISAGENTICOUTPUT - FAILURE MODE TESTS
        // ============================================================

        [Fact]
        public void Normalise_EmptyJson_ReturnsFalseIsSuccess()
        {
            var response = _actions.NormaliseAgenticOutput(string.Empty, "DOC");

            Assert.False(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.ErrorDetail));
            Assert.Empty(response.Chunks);
        }

        [Fact]
        public void Normalise_NullJson_ReturnsFalseIsSuccess()
        {
            var response = _actions.NormaliseAgenticOutput(null!, "DOC");

            Assert.False(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.ErrorDetail));
        }

        [Fact]
        public void Normalise_MalformedJson_ReturnsFalseIsSuccess()
        {
            var response = _actions.NormaliseAgenticOutput(
                "this is not json at all", "DOC");

            Assert.False(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.ErrorDetail));
            Assert.Empty(response.Chunks);
        }

        [Fact]
        public void Normalise_EmptyArray_ReturnsFalseIsSuccess()
        {
            var response = _actions.NormaliseAgenticOutput("[]", "DOC");

            Assert.False(response.IsSuccess);
            Assert.False(string.IsNullOrEmpty(response.ErrorDetail));
        }

        [Fact]
        public void Normalise_MissingCategoryField_SkipsObjectContinuesRest()
        {
            string json = """
                [
                  {"facts": ["Orphaned fact."]},
                  {"category": "Genomics", "facts": ["Valid fact."]}
                ]
                """;

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.True(response.IsSuccess);
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Genomics", response.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void Normalise_MissingFactsField_SkipsObjectContinuesRest()
        {
            string json = """
                [
                  {"category": "Battery Chemistry"},
                  {"category": "Genomics", "facts": ["Valid fact."]}
                ]
                """;

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.True(response.IsSuccess);
            Assert.Equal(1, response.TotalChunks);
            Assert.Equal("Genomics", response.Chunks[0].ThematicCategory);
        }

        [Fact]
        public void Normalise_EmptyFactsArray_SkipsObject()
        {
            string json = """
                [
                  {"category": "Battery Chemistry", "facts": []},
                  {"category": "Genomics", "facts": ["Valid fact."]}
                ]
                """;

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.True(response.IsSuccess);
            Assert.Equal(1, response.TotalChunks);
        }

        [Fact]
        public void Normalise_EmptyDocumentId_DefaultsToDocUnknown()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Genomics", new[] { "Valid fact." })
            });

            var response = _actions.NormaliseAgenticOutput(json, string.Empty);

            Assert.True(response.IsSuccess);
            Assert.StartsWith("DOC-UNKNOWN", response.Chunks[0].ChunkId);
        }

        [Fact]
        public void Normalise_MarkdownFencesInResponse_AreStrippedBeforeParsing()
        {
            string json = "```json\n" +
                          "[{\"category\": \"Genomics\", \"facts\": [\"Valid fact.\"]}]\n" +
                          "```";

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.True(response.IsSuccess,
                $"Expected IsSuccess true after stripping fences. ErrorDetail: {response.ErrorDetail}");
            Assert.Equal(1, response.TotalChunks);
        }

        [Fact]
        public void Normalise_SingleProposition_PropositionCountIsOne()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Urban Planning", new[] { "The floor area ratio regulates building density." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "DOC");

            Assert.True(response.IsSuccess);
            Assert.Equal(1, response.Chunks[0].PropositionCount);
        }

        [Fact]
        public void Normalise_DocumentId_IsEmbeddedInEachChunk()
        {
            string json = BuildSimpleGroupingJson(new[]
            {
                ("Genomics", new[] { "Fact one." }),
                ("Cloud Infrastructure", new[] { "Fact two." })
            });

            var response = _actions.NormaliseAgenticOutput(json, "TESTDOC-99");

            Assert.All(response.Chunks, chunk =>
                Assert.Equal("TESTDOC-99", chunk.DocumentId));
        }

        // ============================================================
        // HELPERS
        // ============================================================

        // Concatenates the text of all non-empty input chunks separated by
        // double newlines, as the source text for PreChunkForExtraction.
        private static string BuildSourceText(TestCase tc)
        {
            if (tc.Input?.Chunks == null || tc.Input.Chunks.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var chunk in tc.Input.Chunks)
            {
                if (!string.IsNullOrWhiteSpace(chunk.Text))
                {
                    sb.AppendLine(chunk.Text.Trim());
                    sb.AppendLine();
                }
            }
            return sb.ToString().Trim();
        }

        // Builds a synthetic LLM grouping response JSON from the expectedGroupingOutput
        // of a test case. Distributes propositions from expectedExtractionOutput evenly
        // across the themes. Falls back to a placeholder fact per theme when propositions
        // are absent (integration test cases).
        private static string BuildSyntheticGroupingJson(TestCase tc)
        {
            var groups = new List<object>();
            var themes = tc.ExpectedGroupingOutput?.Themes ?? new List<string> { "General" };
            var propositions = tc.ExpectedExtractionOutput?.Propositions ?? new List<string>();
            int chunkCount = tc.ExpectedGroupingOutput?.ChunkCount ?? 1;

            if (propositions.Count > 0 && themes.Count > 0)
            {
                int perTheme = Math.Max(1, propositions.Count / themes.Count);
                int propIndex = 0;

                for (int i = 0; i < themes.Count; i++)
                {
                    var facts = new List<string>();
                    int take = (i == themes.Count - 1)
                        ? propositions.Count - propIndex
                        : perTheme;

                    for (int j = 0; j < take && propIndex < propositions.Count; j++)
                        facts.Add(propositions[propIndex++]);

                    if (facts.Count > 0)
                        groups.Add(new { category = themes[i], facts });
                }
            }
            else
            {
                foreach (var theme in themes)
                {
                    groups.Add(new
                    {
                        category = theme,
                        facts = new[] { $"Placeholder fact for {theme}." }
                    });
                }
            }

            return JsonSerializer.Serialize(groups);
        }

        // Builds a grouping JSON directly from supplied tuples.
        // Used for field-value and failure-mode tests that do not reference a
        // specific test case from the JSON file.
        private static string BuildSimpleGroupingJson(
            IEnumerable<(string Category, string[] Facts)> groups)
        {
            var list = groups.Select(g => new { category = g.Category, facts = g.Facts });
            return JsonSerializer.Serialize(list);
        }
    }
}
