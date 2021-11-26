using Bot_Dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bot_Dashboard.Controllers
{
	public class HomeController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Invite()
		{

			return Redirect("https://discord.com/oauth2/authorize?" +
				"client_id=" + HostConfig.Discord.ClientID + '&' +
				"scope=bot&" +
				"permissions=137439239200");
		}

		public IActionResult Support()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}