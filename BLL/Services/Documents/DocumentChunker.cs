using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Services.Documents;

public static class DocumentChunker
{
    private static readonly Regex ChapterHeaderRegex = new Regex(
        @"^(?:Chương|Chapter|Phần|Part)\s+(\d+|[IVXLCDM]+)(?:\s*[:-]\s*|\s|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TocIndicatorRegex = new Regex(
        @"^(?:Mục\s+lục|Table\s+of\s+contents|TOC|Content|Contents)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsTocLine(string paragraph)
    {
        var trimmed = paragraph.Trim();
        
        // Chứa các ký tự chấm lửng kéo dài thường dùng nối tiêu đề với số trang trong mục lục
        if (trimmed.Contains("...") || trimmed.Contains("..") || trimmed.Contains("___") || trimmed.Contains("---"))
        {
            return true;
        }

        // Kết thúc bằng số trang dạng: "Chương 1: Giới thiệu 12" hoặc "Chương 1: Giới thiệu trang 12"
        if (Regex.IsMatch(trimmed, @"\s+\d+$") || Regex.IsMatch(trimmed, @"\b(?:trang|page)\s+\d+$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsChapterHeader(string paragraph)
    {
        var trimmed = paragraph.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 120)
        {
            return false;
        }

        // Tiêu đề chương không bao giờ kết thúc bằng dấu phẩy
        if (trimmed.EndsWith(","))
        {
            return false;
        }

        // Dòng mục lục thì không phải là tiêu đề chương thực tế
        if (IsTocLine(trimmed))
        {
            return false;
        }

        // Phải khớp với định dạng "Chương X", "Chapter X", "Phần X"... ở đầu dòng
        if (!ChapterHeaderRegex.IsMatch(trimmed))
        {
            return false;
        }

        // Vì tiêu đề chương (nằm riêng trên 1 paragraph) thường rất ngắn, khống chế số từ <= 35
        var wordCount = CountWords(trimmed);
        if (wordCount > 35)
        {
            return false;
        }

        return true;
    }

    public static IReadOnlyList<(string Content, int? PageNumber)> ChunkPages(IEnumerable<(string Content, int? PageNumber)> pages, int minWords = 50, int maxWords = 500, int overlapWords = 80)
    {
        var chunks = new List<(string Content, int? PageNumber)>();
        var currentChunk = new List<string>();
        var currentWordCount = 0;
        int? currentStartPage = null;

        var isInToc = false;
        var wordsSinceLastHeader = 10000;

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Content))
            {
                continue;
            }

            var text = page.Content.Replace("\r\n", "\n");
            var paragraphs = Regex.Split(text, @"\n{2,}")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            foreach (var paragraph in paragraphs)
            {
                if (currentChunk.Count == 0)
                {
                    currentStartPage = page.PageNumber;
                }

                var pWordCount = CountWords(paragraph);
                var trimmed = paragraph.Trim();

                if (TocIndicatorRegex.IsMatch(trimmed))
                {
                    isInToc = true;
                }
                else if (isInToc && pWordCount > 80)
                {
                    isInToc = false;
                }

                var isNewChapter = false;
                if (!isInToc && IsChapterHeader(paragraph))
                {
                    if (wordsSinceLastHeader >= 30)
                    {
                        isNewChapter = true;
                    }
                }

                if (isNewChapter && currentChunk.Count > 0)
                {
                    if (currentWordCount >= minWords)
                    {
                        chunks.Add((string.Join("\n\n", currentChunk), currentStartPage));
                        currentChunk = new List<string>();
                        currentWordCount = 0;
                        wordsSinceLastHeader = 0;
                        currentStartPage = page.PageNumber;
                    }
                }

                if (currentWordCount + pWordCount <= maxWords)
                {
                    currentChunk.Add(paragraph);
                    currentWordCount += pWordCount;
                }
                else
                {
                    // Đã vượt quá maxWords -> Đóng chunk hiện tại (maxWords là giới hạn cứng)
                    if (currentChunk.Count > 0)
                    {
                        chunks.Add((string.Join("\n\n", currentChunk), currentStartPage));
                        currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                        currentStartPage = page.PageNumber; 
                    }

                    if (currentWordCount + pWordCount <= maxWords)
                    {
                        currentChunk.Add(paragraph);
                        currentWordCount += pWordCount;
                    }
                    else
                    {
                        // Paragraph quá dài (kể cả khi đã tạo chunk mới), cần cắt theo câu
                        var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();

                        foreach (var sentence in sentences)
                        {
                            var sWordCount = CountWords(sentence);
                            if (currentWordCount + sWordCount <= maxWords)
                            {
                                currentChunk.Add(sentence);
                                currentWordCount += sWordCount;
                            }
                            else
                            {
                                // Câu này khi thêm vào vượt maxWords -> Đóng chunk
                                if (currentChunk.Count > 0)
                                {
                                    chunks.Add((string.Join(" ", currentChunk), currentStartPage));
                                    currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                                    currentStartPage = page.PageNumber;
                                }
                                
                                if (currentWordCount + sWordCount <= maxWords)
                                {
                                    currentChunk.Add(sentence);
                                    currentWordCount += sWordCount;
                                }
                                else
                                {
                                    // Bản thân câu đã dài hơn maxWords -> Bắt buộc cắt theo từ (hard limit)
                                    var words = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                                    var start = 0;

                                    while (start < words.Length)
                                    {
                                        var spaceLeft = maxWords - currentWordCount;
                                        if (spaceLeft <= 0)
                                        {
                                            chunks.Add((string.Join(" ", currentChunk), currentStartPage));
                                            currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                                            currentStartPage = page.PageNumber;
                                            spaceLeft = maxWords - currentWordCount;
                                        }

                                        var length = Math.Min(spaceLeft, words.Length - start);
                                        var wordPart = string.Join(" ", words.Skip(start).Take(length));
                                        
                                        currentChunk.Add(wordPart);
                                        currentWordCount += length;
                                        start += length;

                                        // Nếu chunk đã đầy thì đóng lại
                                        if (currentWordCount >= maxWords)
                                        {
                                            chunks.Add((string.Join(" ", currentChunk), currentStartPage));
                                            currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                                            currentStartPage = page.PageNumber;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                wordsSinceLastHeader += pWordCount;
            }
        }

        if (currentChunk.Count > 0)
        {
            var remainingContent = string.Join("\n\n", currentChunk);
            if (chunks.Count == 0 || currentWordCount >= minWords)
            {
                chunks.Add((remainingContent, currentStartPage));
            }
            else if (chunks.Count > 0 && currentWordCount < minWords)
            {
                var last = chunks[chunks.Count - 1];
                var lastWordCount = CountWords(last.Content);
                
                if (lastWordCount + currentWordCount <= maxWords)
                {
                    chunks[chunks.Count - 1] = (last.Content + "\n\n" + remainingContent, last.PageNumber);
                }
                else
                {
                    // Adding it would exceed maxWords, so we must add it as a new chunk (maxWords is a hard limit)
                    chunks.Add((remainingContent, currentStartPage));
                }
            }
        }

        return chunks;
    }

    private static int CountWords(string text)
    {
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static List<string> GetOverlap(List<string> previousItems, int overlapWords, out int newWordCount)
    {
        var overlap = new List<string>();
        newWordCount = 0;

        for (int i = previousItems.Count - 1; i >= 0; i--)
        {
            var itemWordCount = CountWords(previousItems[i]);
            // Only keep an item if it doesn't exceed the max overlap, 
            // OR if it's the first item we are keeping AND its size is strictly less than maxWords
            // to avoid infinite loop when spaceLeft <= 0 later
            if (newWordCount + itemWordCount <= overlapWords || (newWordCount == 0 && itemWordCount < 300))
            {
                overlap.Insert(0, previousItems[i]);
                newWordCount += itemWordCount;
            }
            else
            {
                break;
            }
        }

        return overlap;
    }

    public static string ComputeChunkHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
