using Microsoft.AspNetCore.Mvc;

namespace Bot_Dashboard.Controllers
{
	public class ExceptionController : Controller
	{
		public IActionResult Index(int httpstatus = 500)
		{
			return httpstatus switch
			{
				401 => RedirectToAction("Login", "OAuth2"),
				403 => RedirectToAction("Forbidden", "Exception"),
				404 => RedirectToAction("NotFound", "Exception"),
				_ => View()
			};
		}

		public IActionResult NotFound(string url)
		{
			ViewData["URL"] = url;
			return View();
		}

		public IActionResult Forbidden() { return View(); }

		public IActionResult DiscordAuthFailed() { return View(); }
	}
}
