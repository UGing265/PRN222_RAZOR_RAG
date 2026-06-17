namespace BLL.DTOs.Documents;

/// <summary>
/// Input payload for exporting a document-comparison result to PDF.
/// </summary>
public sealed class ComparisonExportRequest
{
    /// <summary>Raw markdown returned by the comparison service.</summary>
    public required string RawMarkdown { get; init; }

    /// <summary>Titles of the documents that were compared (2-5 entries).</summary>
    public required IReadOnlyList<string> DocumentTitles { get; init; }

    /// <summary>Email of the user requesting the export (for the header).</summary>
    public required string RequesterEmail { get; init; }

    /// <summary>UTC timestamp when the comparison was generated.</summary>
    public required DateTime GeneratedAtUtc { get; init; }
}
