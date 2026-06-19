namespace AgenticChunkingLibrary.Helpers
{
    internal static class TokenEstimator
    {
        internal static int Estimate(string text) => text.Length / 4;
    }
}
