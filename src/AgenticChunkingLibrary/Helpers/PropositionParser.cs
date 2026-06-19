using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgenticChunkingLibrary.Helpers
{
    internal static class PropositionParser
    {
        internal static List<string> Parse(string rawJson)
        {
            var empty = new List<string>();
            if (string.IsNullOrWhiteSpace(rawJson)) return empty;

            string trimmed = rawJson.Trim();

            // LLM sometimes wraps the entire response in outer quotes before the fence.
            // Strip them so StripFences can see the backticks.
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
                trimmed = trimmed.Substring(1, trimmed.Length - 2);

            string cleaned = StripFences(trimmed);

            // Try as-is first; if that fails due to doubled quotes (""text""), normalise and retry.
            return TryParseArray(cleaned)
                ?? TryParseArray(cleaned.Replace("\"\"", "\""))
                ?? empty;
        }

        private static string StripFences(string text)
        {
            if (!text.StartsWith("```")) return text;
            int firstNewline = text.IndexOf('\n');
            int lastFence = text.LastIndexOf("```");
            if (firstNewline > 0 && lastFence > firstNewline)
                return text.Substring(firstNewline, lastFence - firstNewline).Trim();
            return text;
        }

        // Returns null on JsonException so the caller can try a normalised variant.
        // Returns an empty list when the JSON is valid but not an array.
        private static List<string>? TryParseArray(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return new List<string>();

                var result = new List<string>();
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    string? value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                }
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
