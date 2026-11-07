using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
