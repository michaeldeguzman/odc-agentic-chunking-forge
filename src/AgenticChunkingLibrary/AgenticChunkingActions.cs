using System;
using System.Collections.Generic;
using System.Text;
using AgenticChunkingLibrary.Helpers;
using AgenticChunkingLibrary.Models;

namespace AgenticChunkingLibrary
{
    public class AgenticChunkingActions : IAgenticChunkingActions
    {
        public List<string> PreChunkForExtraction(string sourceText, int maxTokensPerBatch)
        {
            var batches = new List<string>();

            if (string.IsNullOrWhiteSpace(sourceText)) return batches;
            if (maxTokensPerBatch <= 0) maxTokensPerBatch = 2000;

            string[] paragraphs = sourceText.Split(
                new[] { "\n\n" },
                StringSplitOptions.RemoveEmptyEntries);

            var currentBatch = new StringBuilder();

            foreach (string paragraph in paragraphs)
            {
                string trimmed = paragraph.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                int paragraphTokens = TokenEstimator.Estimate(trimmed);
                int currentTokens = TokenEstimator.Estimate(currentBatch.ToString());

                if (paragraphTokens > maxTokensPerBatch)
                {
                    if (currentBatch.Length > 0)
                    {
                        batches.Add(currentBatch.ToString().Trim());
                        currentBatch.Clear();
                    }

                    string[] sentences = trimmed.Split(
                        new[] { ". " },
                        StringSplitOptions.RemoveEmptyEntries);

                    var sentenceBatch = new StringBuilder();
                    foreach (string sentence in sentences)
                    {
                        string s = sentence.Trim();
                        if (!s.EndsWith(".")) s += ".";

                        if (TokenEstimator.Estimate(sentenceBatch.ToString()) +
                            TokenEstimator.Estimate(s) > maxTokensPerBatch)
                        {
                            if (sentenceBatch.Length > 0)
                            {
                                batches.Add(sentenceBatch.ToString().Trim());
                                sentenceBatch.Clear();
                            }
                        }
                        sentenceBatch.Append(s).Append(" ");
                    }

                    if (sentenceBatch.Length > 0)
                        batches.Add(sentenceBatch.ToString().Trim());

                    continue;
                }

                if (currentTokens + paragraphTokens > maxTokensPerBatch && currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString().Trim());
                    currentBatch.Clear();
                }

                currentBatch.AppendLine(trimmed);
                currentBatch.AppendLine();
            }

            if (currentBatch.Length > 0)
                batches.Add(currentBatch.ToString().Trim());

            return batches;
        }

        public AgenticResponse NormaliseAgenticOutput(string rawGroupingJson, string documentId)
        {
            var response = new AgenticResponse
            {
                Chunks = new List<AgenticChunk>(),
                TotalChunks = 0,
                TotalPropositions = 0,
                TotalTokenEstimate = 0,
                IsSuccess = false,
                ErrorDetail = string.Empty
            };

            if (string.IsNullOrWhiteSpace(documentId))
                documentId = "DOC-UNKNOWN";

            if (string.IsNullOrWhiteSpace(rawGroupingJson))
            {
                response.ErrorDetail = "rawGroupingJson was null or empty.";
                return response;
            }

            var entries = GroupingParser.Parse(rawGroupingJson);

            if (entries.Count == 0)
            {
                response.ErrorDetail = "Grouping JSON parsed to zero entries. The LLM may have returned malformed JSON or an empty array.";
                return response;
            }

            int sequenceCounter = 1;
            int totalPropositions = 0;
            int totalTokens = 0;

            foreach (var entry in entries)
            {
                string mergedContent = string.Join(" ", entry.Facts);
                int charCount = mergedContent.Length;
                int tokenEstimate = TokenEstimator.Estimate(mergedContent);

                var chunk = new AgenticChunk
                {
                    ChunkId = $"{documentId}-{sequenceCounter:D4}",
                    DocumentId = documentId,
                    ThematicCategory = entry.Category,
                    MergedContent = mergedContent,
                    PropositionCount = entry.Facts.Count,
                    CharacterCount = charCount,
                    TokenEstimate = tokenEstimate,
                    Hash = HashHelper.Sha256(mergedContent)
                };

                response.Chunks.Add(chunk);
                totalPropositions += entry.Facts.Count;
                totalTokens += tokenEstimate;
                sequenceCounter++;
            }

            response.TotalChunks = response.Chunks.Count;
            response.TotalPropositions = totalPropositions;
            response.TotalTokenEstimate = totalTokens;
            response.IsSuccess = true;

            return response;
        }
    }
}
