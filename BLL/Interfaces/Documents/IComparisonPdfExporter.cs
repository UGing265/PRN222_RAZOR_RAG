using BLL.DTOs.Documents;

namespace BLL.Interfaces.Documents;

/// <summary>
/// Renders a document-comparison result (markdown) into a downloadable PDF.
/// </summary>
public interface IComparisonPdfExporter
{
    /// <summary>
    /// Build a PDF byte array from the given comparison request.
    /// </summary>
    /// <returns>PDF file content (bytes).</returns>
    byte[] Build(ComparisonExportRequest request);
}
