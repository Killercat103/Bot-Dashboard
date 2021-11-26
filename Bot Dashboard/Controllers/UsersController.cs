using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace Bot_Dashboard.Controllers
{

	public class UsersController : Controller
	{
		protected class ClientUser { public string? Access_Token { get; set; } }

		protected class Guild
		{
			public long ID { get; set; }
			public string? Name { get; set; }
			public string? Icon { get; set; }
			public bool Owner { get; set; }
			public long Permissions { get; set; }
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Guilds()
		{
			string? discordSession = HttpContext.Session.GetString("Discord.Session");

			if (string.IsNullOrEmpty(discordSession)) { return RedirectToAction("Login", "OAuth2"); }

			ClientUser? user = JsonConvert.DeserializeObject<ClientUser>(discordSession);


			RestClient restClient = new();
			RestRequest restRequest = new();

			restRequest.AddHeader("Authorization", "Bearer " + user?.Access_Token);

			restClient.BaseUrl = new Uri("https://discord.com/api/v9/users/@me/guilds");
			IRestResponse? response = restClient.Get(restRequest);

			IList<Guild>? allGuilds = JsonConvert.DeserializeObject<List<Guild>>(response.Content);

			IList<Guild> guilds = new List<Guild>();


			for (int i = 0; i < allGuilds?.Count; ++i)
			{
				Guild? guild = allGuilds[i];

				if (guild != null && ( guild.Owner == true || (guild.Permissions | 0x20L) == guild.Permissions )) { guilds.Add(guild); }
			}

			ViewData["Guilds"] = JsonConvert.SerializeObject(guilds, Formatting.Indented);

			return View();
		}
	}
}
