using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgenticChunkingLibrary.Helpers
{
    internal static class PropositionParser
    {
        internal static List<string> Parse(string rawJson)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(rawJson)) return result;

            try
            {
                string cleaned = rawJson.Trim();
                if (cleaned.StartsWith("```"))
                {
                    int firstNewline = cleaned.IndexOf('\n');
                    int lastFence = cleaned.LastIndexOf("```");
                    if (firstNewline > 0 && lastFence > firstNewline)
                        cleaned = cleaned.Substring(firstNewline, lastFence - firstNewline).Trim();
                }

                using JsonDocument doc = JsonDocument.Parse(cleaned);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;

                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    string? value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                }
            }
            catch (Exception)
            {
                // Return empty list on any parse failure
            }

            return result;
        }
    }
}
