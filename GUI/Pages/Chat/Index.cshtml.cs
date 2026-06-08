using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GUI.Pages.Chat;

/// <summary>
/// Thin shell — the chat surface talks to <c>/api/chat/*</c> (Minimal API)
/// via <see cref="GUI.Endpoints.ChatEndpoints"/>. This page exists only to
/// render the chat UI shell and enforce role authorization on the GET.
/// </summary>
[Authorize(Roles = "Lecturer,Student")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
