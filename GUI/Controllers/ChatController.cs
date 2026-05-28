using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Authorize(Roles = "Admin")]
public class ChatController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return NotFound();
    }
}
