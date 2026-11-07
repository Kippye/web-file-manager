using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public ActionResult AntiforgeryError()
        {
            return View("Index", new ErrorViewModel { Message = "Error uploading form", Details = "Your data could not be accepted. You may need to log in again.", RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
