using Microsoft.AspNetCore.Mvc;

namespace Bot_Dashboard.Controllers
{
	public class ExceptionController : Controller
	{
		public IActionResult Index(int httpStatus = 500)
		{
			return httpStatus switch
			{
				401 => RedirectToAction("Login", "OAuth2"),
				403 => RedirectToAction("Forbidden", "Exception"),
				404 => RedirectToAction("NotFound", "Exception"),
				429 => RedirectToAction("TooManyRequests", "Exception"),
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
		
		public IActionResult TooManyRequests() { return View(); }
	}
}
