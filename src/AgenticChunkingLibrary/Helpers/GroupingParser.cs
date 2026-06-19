using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgenticChunkingLibrary.Helpers
{
    internal record GroupingEntry(string Category, List<string> Facts);

    internal static class GroupingParser
    {
        internal static List<GroupingEntry> Parse(string rawJson)
        {
            var result = new List<GroupingEntry>();
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
                    if (!element.TryGetProperty("category", out JsonElement categoryEl)) continue;
                    if (!element.TryGetProperty("facts", out JsonElement factsEl)) continue;
                    if (factsEl.ValueKind != JsonValueKind.Array) continue;

                    string? category = categoryEl.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(category)) continue;

                    var facts = new List<string>();
                    foreach (JsonElement fact in factsEl.EnumerateArray())
                    {
                        string? value = fact.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            facts.Add(value.Trim());
                    }

                    if (facts.Count > 0)
                        result.Add(new GroupingEntry(category, facts));
                }
            }
            catch (Exception)
            {
                // Return whatever was parsed before the failure
            }

            return result;
        }
    }
}
