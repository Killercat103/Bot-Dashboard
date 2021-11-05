using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bot_Dashboard.Controllers
{
	[Authorize(AuthenticationSchemes = "Discord")]
	public class UsersController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public string Login()
		{
			string id = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
			return id;
		}

		[AllowAnonymous]
		public IActionResult DiscordAuthFailed()
		{
			return View();
		}
	}
}
