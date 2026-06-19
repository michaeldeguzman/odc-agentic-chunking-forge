using System.Collections.Generic;
using AgenticChunkingLibrary;
using Xunit;

namespace AgenticChunkingLibrary.Tests
{
    public class ParsePropositionsTests
    {
        private readonly AgenticChunkingActions _sut = new();

        [Fact]
        public void ValidJsonArray_ReturnsCorrectListOfStrings()
        {
            var result = _sut.ParsePropositions("[\"Proposition one.\", \"Proposition two.\"]");
            Assert.Equal(2, result.Count);
            Assert.Equal("Proposition one.", result[0]);
            Assert.Equal("Proposition two.", result[1]);
        }

        [Fact]
        public void EmptyString_ReturnsEmptyList()
        {
            var result = _sut.ParsePropositions(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void NullInput_ReturnsEmptyList()
        {
            var result = _sut.ParsePropositions(null!);
            Assert.Empty(result);
        }

        [Fact]
        public void MalformedJson_ReturnsEmptyListWithoutThrowing()
        {
            var result = _sut.ParsePropositions("this is { not } valid [ json");
            Assert.Empty(result);
        }

        [Fact]
        public void MarkdownFencesWithLanguageTag_AreStrippedBeforeParsing()
        {
            string input = "```json\n[\"Proposition one.\", \"Proposition two.\"]\n```";
            var result = _sut.ParsePropositions(input);
            Assert.Equal(2, result.Count);
            Assert.Equal("Proposition one.", result[0]);
        }

        [Fact]
        public void MarkdownFencesWithoutLanguageTag_AreStrippedBeforeParsing()
        {
            string input = "```\n[\"Proposition one.\"]\n```";
            var result = _sut.ParsePropositions(input);
            Assert.Single(result);
            Assert.Equal("Proposition one.", result[0]);
        }

        [Fact]
        public void DoubledInnerQuotes_AreNormalisedToSingleQuotes()
        {
            // LLM sometimes wraps each element with "" instead of "
            string input = "[\"\"Proposition one.\"\", \"\"Proposition two.\"\"]";
            var result = _sut.ParsePropositions(input);
            Assert.Equal(2, result.Count);
            Assert.Equal("Proposition one.", result[0]);
            Assert.Equal("Proposition two.", result[1]);
        }

        [Fact]
        public void WhitespaceOnlyStringsInArray_AreExcludedFromResult()
        {
            string input = "[\"Valid proposition.\", \"   \", \"\", \"Another valid one.\"]";
            var result = _sut.ParsePropositions(input);
            Assert.Equal(2, result.Count);
            Assert.Equal("Valid proposition.", result[0]);
            Assert.Equal("Another valid one.", result[1]);
        }

        [Fact]
        public void SingleItemArray_ReturnsListWithOneString()
        {
            var result = _sut.ParsePropositions("[\"Only proposition.\"]");
            Assert.Single(result);
            Assert.Equal("Only proposition.", result[0]);
        }

        [Fact]
        public void FourItemArray_ReturnsListWithFourStrings()
        {
            string input = "[\"One.\", \"Two.\", \"Three.\", \"Four.\"]";
            var result = _sut.ParsePropositions(input);
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void LeadingAndTrailingWhitespace_IsTrimmedFromEachProposition()
        {
            string input = "[\"  Proposition with padding.  \", \"  Another one.  \"]";
            var result = _sut.ParsePropositions(input);
            Assert.Equal("Proposition with padding.", result[0]);
            Assert.Equal("Another one.", result[1]);
        }

        [Fact]
        public void JsonObject_ReturnsEmptyList()
        {
            var result = _sut.ParsePropositions("{\"category\": \"test\", \"facts\": [\"fact\"]}");
            Assert.Empty(result);
        }

        [Fact]
        public void PlainString_ReturnsEmptyList()
        {
            var result = _sut.ParsePropositions("This is just a plain sentence, not JSON.");
            Assert.Empty(result);
        }

        [Fact]
        public void PropositionsWithEscapedCharacters_AreHandledCorrectly()
        {
            string input = "[\"He said \\\"hello\\\" to the group.\", \"Cost was \\u00a3100.\"]";
            var result = _sut.ParsePropositions(input);
            Assert.Equal(2, result.Count);
            Assert.Equal("He said \"hello\" to the group.", result[0]);
            Assert.Equal("Cost was £100.", result[1]);
        }
    }
}
