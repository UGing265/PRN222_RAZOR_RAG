using System.Security.Cryptography;
using System.Text;

namespace BLL.Services.Documents;

public static class DocumentChunker
{
    public static IReadOnlyList<string> ChunkText(string text, int minWords = 1, int maxWords = 60, int overlapWords = 10)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var words = text
            .Replace("\r\n", "\n")
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (words.Count <= maxWords)
        {
            return [string.Join(' ', words)];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < words.Count)
        {
            var length = Math.Min(maxWords, words.Count - start);
            if (length < minWords)
            {
                break;
            }

            var chunk = string.Join(' ', words.Skip(start).Take(length));
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (start + length >= words.Count)
            {
                break;
            }

            start += Math.Max(1, length - overlapWords);
        }

        return chunks;
    }

    public static string ComputeChunkHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
