using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace Bot_Dashboard.Controllers
{
	public class DashboardController : Controller
	{
		public IActionResult Index(long guildID)
		{
			return View();
		}
	}
}