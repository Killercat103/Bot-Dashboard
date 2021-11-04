using Microsoft.AspNetCore.Mvc;

namespace Bot_Dashboard.Controllers
{
	public class UsersController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public string Login(string code)
		{
			return code;
		}
	}
}
