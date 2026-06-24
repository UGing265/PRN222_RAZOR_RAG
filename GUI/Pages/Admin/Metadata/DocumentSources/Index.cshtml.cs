using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin.Metadata.DocumentSources;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Categories", new { tab = "documentsources" });
    }
}
