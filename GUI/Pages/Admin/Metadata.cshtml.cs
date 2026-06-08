using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin;

/// <summary>
/// Backwards-compatibility redirect: the old god page at /Admin/Metadata
/// (with ?tab=subjects, ?tab=documenttypes, etc.) is preserved as a stub
/// that redirects to the new split pages under /Admin/Metadata/{Entity}.
/// </summary>
[Authorize(Roles = "Admin")]
public class MetadataModel : PageModel
{
    public IActionResult OnGet(string? tab)
    {
        var target = tab?.ToLowerInvariant() switch
        {
            "subjects"         => "/Admin/Metadata/Subjects",
            "documenttypes"    => "/Admin/Metadata/DocumentTypes",
            "languages"        => "/Admin/Metadata/Languages",
            "documentsources"  => "/Admin/Metadata/DocumentSources",
            "academicterms"    => "/Admin/Metadata/AcademicTerms",
            _                  => "/Admin/Metadata/Subjects",
        };
        return RedirectToPage(target);
    }
}
