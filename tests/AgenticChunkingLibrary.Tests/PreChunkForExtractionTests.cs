using System;
using System.Collections.Generic;
using System.Linq;
using AgenticChunkingLibrary;
using Xunit;

namespace AgenticChunkingLibrary.Tests
{
    public class PreChunkForExtractionTests
    {
        private readonly AgenticChunkingActions _sut = new();

        [Fact]
        public void EmptyString_ReturnsEmptyList()
        {
            var result = _sut.PreChunkForExtraction("", 2000);
            Assert.Empty(result);
        }

        [Fact]
        public void NullInput_ReturnsEmptyList()
        {
            var result = _sut.PreChunkForExtraction(null!, 2000);
            Assert.Empty(result);
        }

        [Fact]
        public void SingleParagraphUnderLimit_ReturnsOneBatch()
        {
            string text = "This is a single short paragraph.";
            var result = _sut.PreChunkForExtraction(text, 2000);
            Assert.Single(result);
            Assert.Contains("This is a single short paragraph.", result[0]);
        }

        [Fact]
        public void TwoParagraphsBothUnderLimit_ReturnsOneBatch()
        {
            string text = "First paragraph.\n\nSecond paragraph.";
            var result = _sut.PreChunkForExtraction(text, 2000);
            Assert.Single(result);
            Assert.Contains("First paragraph.", result[0]);
            Assert.Contains("Second paragraph.", result[0]);
        }

        [Fact]
        public void TwoParagraphsExceedingLimit_ReturnsTwoBatches()
        {
            // Each paragraph is ~250 chars = 62 tokens; limit set to 80 so combined exceeds it
            string para1 = new string('A', 250);
            string para2 = new string('B', 250);
            string text = $"{para1}\n\n{para2}";

            var result = _sut.PreChunkForExtraction(text, 80);

            Assert.Equal(2, result.Count);
            Assert.Contains(para1, result[0]);
            Assert.Contains(para2, result[1]);
        }

        [Fact]
        public void OversizedParagraph_IsSplitBySentenceBoundary()
        {
            // Build a paragraph with many sentences, each ~100 chars = 25 tokens; limit = 30
            string sentence = new string('X', 96) + "y"; // 97 chars, ensure ends with non-period
            string text = $"{sentence}. {sentence}. {sentence}.";

            var result = _sut.PreChunkForExtraction(text, 30);

            Assert.True(result.Count > 1, "Oversized paragraph should be split into multiple batches");
            foreach (string batch in result)
                Assert.True(batch.Length / 4 <= 30 + 30,
                    "Each batch should be near or at the token limit");
        }

        [Fact]
        public void ZeroMaxTokens_DefaultsTo2000()
        {
            string text = "Short text.";
            var result = _sut.PreChunkForExtraction(text, 0);
            Assert.Single(result);
        }

        [Fact]
        public void ParagraphsSeparatedByDoubleNewline_TreatedAsSeparateUnits()
        {
            // Set limit just above one paragraph so they can't be combined
            string para1 = new string('A', 80); // 20 tokens
            string para2 = new string('B', 80); // 20 tokens
            string text = $"{para1}\n\n{para2}";

            // Limit of 25 fits one but not two (combined = 40+ tokens)
            var result = _sut.PreChunkForExtraction(text, 25);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void WhitespaceOnlyParagraphs_AreSkipped()
        {
            string text = "Real content.\n\n   \n\nMore content.";
            var result = _sut.PreChunkForExtraction(text, 2000);
            Assert.Single(result);
            Assert.Contains("Real content.", result[0]);
            Assert.Contains("More content.", result[0]);
        }

        [Fact]
        public void ParagraphExactlyAtLimit_IncludedInCurrentBatch()
        {
            // 40 chars = exactly 10 tokens; limit = 10
            string para = new string('A', 40);
            var result = _sut.PreChunkForExtraction(para, 10);
            Assert.Single(result);
            Assert.Contains(para, result[0]);
        }
    }
}
