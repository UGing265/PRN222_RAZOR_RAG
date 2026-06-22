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

    public static IReadOnlyList<string> ChunkText(string text, int minWords = 1, int maxWords = 1100, int overlapWords = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        // Chuẩn hóa dòng
        text = text.Replace("\r\n", "\n");

        // Bước 1: Chia nhỏ theo đoạn văn (\n\n)
        var paragraphs = Regex.Split(text, @"\n{2,}")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var chunks = new List<string>();
        var currentChunk = new List<string>();
        var currentWordCount = 0;

        var isInToc = false;
        var wordsSinceLastHeader = 10000; // Khởi tạo lớn để chấp nhận tiêu đề chương đầu tiên

        foreach (var paragraph in paragraphs)
        {
            var pWordCount = CountWords(paragraph);
            var trimmed = paragraph.Trim();

            // Nhận diện bắt đầu Mục lục
            if (TocIndicatorRegex.IsMatch(trimmed))
            {
                isInToc = true;
            }
            // Nếu gặp một đoạn văn dài (> 80 từ), chứng tỏ đã hết Mục lục và vào nội dung chính
            else if (isInToc && pWordCount > 80)
            {
                isInToc = false;
            }

            // Chỉ nhận diện tiêu đề chương khi không nằm trong mục lục
            var isNewChapter = false;
            if (!isInToc && IsChapterHeader(paragraph))
            {
                // Khoảng cách từ tiêu đề chương trước đến tiêu đề chương này phải đủ lớn (tránh nhận nhầm danh sách mục lục/tóm tắt gần nhau)
                if (wordsSinceLastHeader >= 30)
                {
                    isNewChapter = true;
                }
            }

            // Nếu gặp tiêu đề chương mới và chunk hiện tại đã có dữ liệu -> Ép đóng chunk ngay lập tức
            if (isNewChapter && currentChunk.Count > 0)
            {
                chunks.Add(string.Join("\n\n", currentChunk));
                currentChunk = new List<string>();
                currentWordCount = 0;
                wordsSinceLastHeader = 0;
            }

            // Nếu đoạn văn hiện tại nhét vừa vào chunk đang có
            if (currentWordCount + pWordCount <= maxWords)
            {
                currentChunk.Add(paragraph);
                currentWordCount += pWordCount;
            }
            else
            {
                // Nếu chunk đang có đã có dữ liệu, lưu nó lại thành 1 chunk hoàn chỉnh
                if (currentChunk.Count > 0)
                {
                    chunks.Add(string.Join("\n\n", currentChunk));
                    
                    // Tạo chunk mới có chứa Overlap (lấy đoạn văn cuối của chunk cũ gối sang)
                    currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                }

                // Nếu riêng đoạn văn hiện tại mà đã lớn hơn maxWords, phải chẻ nó theo câu
                if (pWordCount > maxWords)
                {
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
                            if (currentChunk.Count > 0)
                            {
                                chunks.Add(string.Join(" ", currentChunk));
                                currentChunk = GetOverlap(currentChunk, overlapWords, out currentWordCount);
                            }
                            
                            // Nếu 1 câu mà vượt quá maxWords (rất hiếm), cắt cứng theo từ
                            if (sWordCount > maxWords)
                            {
                                var words = sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                                var start = 0;
                                var stride = maxWords - overlapWords;
                                if (stride <= 0) stride = 1;

                                while (start < words.Length)
                                {
                                    var length = Math.Min(maxWords, words.Length - start);
                                    chunks.Add(string.Join(" ", words.Skip(start).Take(length)));
                                    
                                    if (start + length >= words.Length)
                                    {
                                        break;
                                    }
                                    
                                    start += stride;
                                }
                                currentChunk.Clear();
                                currentWordCount = 0;
                            }
                            else
                            {
                                currentChunk.Add(sentence);
                                currentWordCount += sWordCount;
                            }
                        }
                    }
                }
                else
                {
                    currentChunk.Add(paragraph);
                    currentWordCount += pWordCount;
                }
            }

            // Tăng số từ kể từ tiêu đề chương cuối cùng
            wordsSinceLastHeader += pWordCount;
        }

        if (currentChunk.Count > 0 && currentWordCount >= minWords)
        {
            chunks.Add(string.Join("\n\n", currentChunk));
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
            if (newWordCount + itemWordCount <= overlapWords || newWordCount == 0) // Ít nhất giữ lại 1 item
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
