namespace GUI.Models.Documents;

public class AllDocumentsViewModel
{
    public int TotalDocuments { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Query { get; set; }
    public string? SortBy { get; set; }
    public List<DocumentListItemViewModel> Documents { get; set; } = new();
}
