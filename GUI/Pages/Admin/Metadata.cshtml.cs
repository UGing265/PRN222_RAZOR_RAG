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
            "subjects"         => "/Admin/Metadata/Subjects/Index",
            "documenttypes"    => "/Admin/Metadata/DocumentTypes/Index",
            "languages"        => "/Admin/Metadata/Languages/Index",
            "documentsources"  => "/Admin/Metadata/DocumentSources/Index",
            "academicterms"    => "/Admin/Metadata/AcademicTerms/Index",
            _                  => "/Admin/Metadata/Subjects/Index",
        };
        return RedirectToPage(target);
    }
}
