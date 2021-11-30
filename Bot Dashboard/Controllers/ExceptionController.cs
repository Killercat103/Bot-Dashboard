using Microsoft.AspNetCore.Mvc;

namespace Bot_Dashboard.Controllers
{
	public class ExceptionController : Controller
	{
		public IActionResult URL404(string url)
		{
			ViewData["URL"] = url;
			return View();
		}

		public IActionResult Forbidden() { return View(); }

		public IActionResult DiscordAuthFailed() { return View(); }
	}
}
