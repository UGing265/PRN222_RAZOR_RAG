namespace GUI.Models.Documents;

public class MyDocumentsViewModel
{
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int TotalFiles { get; set; }
    public int TotalChunks { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<DocumentListItemViewModel> Documents { get; set; } = new();
    public List<UploadJobViewModel> ActiveUploadJobs { get; set; } = new();
    public List<DAL.Entities.DocumentReport> PendingReports { get; set; } = new();
}
