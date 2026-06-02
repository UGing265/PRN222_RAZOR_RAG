using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Authorize(Roles = "Lecturer,Student")]
public class ChatController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
