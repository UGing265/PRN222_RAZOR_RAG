using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Admin;


[Authorize(Roles = "Admin")]
public class MetadataModel : PageModel
{
    public IActionResult OnGet(string? tab)
    {
        return RedirectToPage("/Admin/Categories");
    }
}
