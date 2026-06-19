using System;
using System.Security.Cryptography;
using System.Text;

namespace AgenticChunkingLibrary.Helpers
{
    internal static class HashHelper
    {
        internal static string Sha256(string input)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return $"sha256-{Convert.ToHexString(hashBytes).ToLower()}";
        }
    }
}
