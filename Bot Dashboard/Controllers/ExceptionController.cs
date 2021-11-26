using Microsoft.AspNetCore.Mvc;

namespace Bot_Dashboard.Controllers
{
    public class ExceptionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult URL404()
        {
            return View();
        }

        public IActionResult DiscordAuthFailed()
        {
            return View();
        }
    }
}
