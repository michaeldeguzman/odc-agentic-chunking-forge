using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AgenticChunkingLibrary.Tests
{
    public class TestSuiteFixture
    {
        public TestSuite Suite { get; }

        public TestSuiteFixture()
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "AgenticTestCases.json");

            string json = File.ReadAllText(path);

            Suite = JsonSerializer.Deserialize<TestSuite>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new FileNotFoundException(
                    "AgenticTestCases.json could not be deserialised.");
        }

        public TestCase GetCase(string id) =>
            Suite.TestCases.Find(tc => tc.Id == id)
            ?? throw new KeyNotFoundException(
                $"Test case {id} not found in AgenticTestCases.json");

        public List<TestCase> GetCategory(string category) =>
            Suite.TestCases.FindAll(tc => tc.Category == category);
    }
}
