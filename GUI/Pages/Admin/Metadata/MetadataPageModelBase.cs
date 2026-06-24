using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using GUI.Pages.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin.Metadata;

/// <summary>
/// Base class for the 5 split Metadata admin pages. Provides common
/// TempData helpers, an OnGet loader for lookups used in creation forms
/// (AcademicTerms are needed by the Subjects create form), and the
/// [Authorize(Roles = "Admin")] attribute.
/// Subclasses are responsible for loading their own entity list.
/// </summary>
[Authorize(Roles = "Admin")]
public abstract class MetadataPageModelBase : PageModel
{
    protected readonly IDocumentService DocumentService;
    protected readonly ILogger Logger;

    protected MetadataPageModelBase(IDocumentService documentService, ILogger logger)
    {
        DocumentService = documentService;
        Logger = logger;
    }

    /// <summary>Shared lookups required by the Subjects create form (and similar).</summary>
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();

    public int SubjectsCount { get; set; }
    public int DocumentTypesCount { get; set; }
    public int LanguagesCount { get; set; }
    public int DocumentSourcesCount { get; set; }
    public int AcademicTermsCount { get; set; }

    public virtual async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var subjects = await DocumentService.GetSubjectsAsync(cancellationToken);
        var terms = await DocumentService.GetAcademicTermsAsync(cancellationToken);
        var langs = await DocumentService.GetLanguagesAsync(cancellationToken);
        var types = await DocumentService.GetDocumentTypesAsync(cancellationToken);
        var sources = await DocumentService.GetDocumentSourcesAsync(cancellationToken);

        AcademicTerms = terms;
        
        SubjectsCount = subjects.Count;
        AcademicTermsCount = terms.Count;
        LanguagesCount = langs.Count;
        DocumentTypesCount = types.Count;
        DocumentSourcesCount = sources.Count;

        return Page();
    }

    protected void SetError(string message) => TempData["ErrorMessage"] = message;
    protected void SetSuccess(string message) => TempData["SuccessMessage"] = message;

    /// <summary>Wrap a Create call: success sets success message, InvalidOperationException surfaces the service error.</summary>
    protected async Task<IActionResult> ExecuteCreateAsync(
        Func<Task> action,
        string successMessage,
        string redirectPage)
    {
        try
        {
            await action();
            SetSuccess(successMessage);
            var notificationService = HttpContext.RequestServices.GetRequiredService<BLL.Interfaces.Notifications.INotificationService>();
            await notificationService.SendMetadataUpdatedAsync("Metadata", "Create", new { }, default);
        }
        catch (InvalidOperationException ex)
        {
            SetError(ex.Message);
        }
        return RedirectToPage(redirectPage);
    }

    /// <summary>Wrap an Update call where service returns a non-null DTO on success and null when not found.</summary>
    protected async Task<IActionResult> ExecuteUpdateAsync(
        Func<Task<object?>> action,
        string notFoundMessage,
        string successMessage,
        string redirectPage)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                SetError(notFoundMessage);
            }
            else
            {
                SetSuccess(successMessage);
                var notificationService = HttpContext.RequestServices.GetRequiredService<BLL.Interfaces.Notifications.INotificationService>();
                await notificationService.SendMetadataUpdatedAsync("Metadata", "Update", new { }, default);
            }
        }
        catch (InvalidOperationException ex)
        {
            SetError(ex.Message);
        }
        return RedirectToPage(redirectPage);
    }

    /// <summary>Wrap a Delete call: true sets success, false sets not-found, exception logs + sets generic error.</summary>
    protected async Task<IActionResult> ExecuteDeleteAsync(
        Func<Task<bool>> action,
        string notFoundMessage,
        string successMessage,
        string genericErrorMessage,
        string entityName,
        Guid id,
        string redirectPage)
    {
        try
        {
            var ok = await action();
            if (ok)
            {
                SetSuccess(successMessage);
                var notificationService = HttpContext.RequestServices.GetRequiredService<BLL.Interfaces.Notifications.INotificationService>();
                await notificationService.SendMetadataUpdatedAsync("Metadata", "Delete", new { }, default);
            }
            else
            {
                SetError(notFoundMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting {Entity} {Id}", entityName, id);
            SetError(genericErrorMessage);
        }
        return RedirectToPage(redirectPage);
    }
}
