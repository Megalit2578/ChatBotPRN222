using System.Text.RegularExpressions;

namespace ServiceLayer.Services;

public class SlidingWindowChunker : IChunker
{
    public List<(string Text, int Page)> Chunk(List<(int Page, string Text)> pages, int chunkSize = 600, int overlap = 100)
    {
        var chunks = new List<(string Text, int Page)>();

        foreach (var (page, raw) in pages)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var normalized = Regex.Replace(raw, "\\s+", " ").Trim();
            var words = normalized.Split(' ');

            if (words.Length <= chunkSize)
            {
                chunks.Add((normalized, page));
                continue;
            }

            int start = 0;
            while (start < words.Length)
            {
                int end = Math.Min(start + chunkSize, words.Length);
                var chunk = string.Join(' ', words, start, end - start);
                chunks.Add((chunk, page));
                if (end == words.Length) break;
                start += chunkSize - overlap;
            }
        }

        return chunks;
    }
}
