using DSharpPlus;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace Bot_Dashboard.Controllers
{
	public class DashboardController : Controller
	{
		protected class ClientUser {
			public string? Access_Token { get; set; }
			public long ID { get; set; }
		}

		protected class Guild
		{
			public long ID { get; set; }
			public string? Name { get; set; }
			public string? Icon { get; set; }
			public bool Owner { get; set; }
			public long Permissions { get; set; }
		}


		public int CheckForGuildAccess(long guildID)
		{
			string? discordSession = HttpContext.Session.GetString("Discord.Session");
			if (discordSession == null) { return 401; }


			ClientUser? user = JsonConvert.DeserializeObject<ClientUser>(discordSession);

			RestClient restClient = new();
			RestRequest restRequest = new();

			restRequest.AddHeader("Authorization", "Bearer " + user?.Access_Token);

			restClient.BaseUrl = new Uri("https://discord.com/api/v9/users/@me/guilds");
			IRestResponse? response = restClient.Get(restRequest);

			for (int i = 0; i < 5 && response.StatusCode == HttpStatusCode.TooManyRequests; ++i) 
			{
				Thread.Sleep(i * 500);
				response = restClient.Get(restRequest);
			}

			if (response.StatusCode == HttpStatusCode.TooManyRequests) { return 429; }

			IList<Guild>? allGuilds = JsonConvert.DeserializeObject<List<Guild>>(response.Content);


			DiscordClient? client = DiscordConnection.Client;
			long[]? admins = HostConfig.Discord.Admins;

			if (admins == null) { admins = Array.Empty<long>(); }

			if (user == null ) { return 401; }

			if (client == null) { throw new Exception("Client missing value!"); }


			for (int i = 0; i < allGuilds?.Count; ++i)
			{
				Guild? guild = allGuilds[i];

				if (guild != null &&
					guild.ID == guildID &&
					client.Guilds.ContainsKey((ulong)guildID) &&
					admins.Contains(user.ID) &&
					(guild.Owner == true ||
						(guild.Permissions | 0x20L) == guild.Permissions))
				{
					return 200;
				}
			}

			return 403;
		}

		public IActionResult Index(long guildID)
		{
			int guildAccessStatusCode = CheckForGuildAccess(guildID);
			if (guildAccessStatusCode != 200) { return StatusCode(guildAccessStatusCode); }

			return View();
		}

		public IActionResult Embed(long guildID)
		{
			int guildAccessStatusCode = CheckForGuildAccess(guildID);
			if (guildAccessStatusCode != 200) { return StatusCode(guildAccessStatusCode); }
			return View();
		}
	}
}