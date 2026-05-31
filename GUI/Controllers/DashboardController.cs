using Microsoft.AspNetCore.Mvc;

namespace GUI.Controllers;

[Route("dashboard")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
