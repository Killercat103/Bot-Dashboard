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

		public class Guild
		{
			public long ID { get; set; }
			public string? Name { get; set; }
			public string? Icon { get; set; }
			public bool Owner { get; set; }
			public long Permissions { get; set; }
			public IList<string>? Channels { get; set; }
		}

		public int CheckForGuildAccess(long guildID)
		{
			string? discordSession = HttpContext.Session.GetString("Discord.Session");
			if (discordSession == null) { return 401; }

			ViewData["GuildID"] = (ulong)guildID;

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

			if (user == null) { return 401; }

			if (client == null) { throw new Exception("Client missing value!"); }


			for (int i = 0; i < allGuilds?.Count; ++i)
			{
				Guild? guild = allGuilds[i];

				if (guild != null && guild.ID == guildID && client.Guilds.ContainsKey((ulong)guild.ID))
				{
					if ((guild.Permissions | 0x20L) == guild.Permissions ||
						guild.Owner ||
						admins.Contains(user.ID))
					{
						ViewData["GuildName"] = guild.Name;
						return 200;
					}
					else
					{
						return 403;
					}
				}
			}

			return 404;
		}

		public IActionResult Index(long guildID)
		{
			int guildAccessStatusCode = CheckForGuildAccess(guildID);
			if (guildAccessStatusCode != 200)
			{
				Response.StatusCode = guildAccessStatusCode;
				return RedirectToAction("Index", "Exception", new
				{
					httpStatus = guildAccessStatusCode
				});
			}

			return View();
		}

		public IActionResult Embed(long guildID)
		{
			int guildAccessStatusCode = CheckForGuildAccess(guildID);
			if (guildAccessStatusCode != 200)
			{
				Response.StatusCode = guildAccessStatusCode;
				return RedirectToAction("Index", "Exception", new
				{
					httpStatus = guildAccessStatusCode
				});
			}
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Embed(long guildID,
			long ChannelID,
			string message,
			string title,
			string description)
		{
			if ((string.IsNullOrEmpty(message) && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description)) ||
				(guildID | ChannelID) == 0L) { return View(); }

			int guildAccessStatusCode = CheckForGuildAccess(guildID);
			if (guildAccessStatusCode != 200)
			{
				Response.StatusCode = guildAccessStatusCode;
				return RedirectToAction("Index", "Exception", new
				{
					httpStatus = guildAccessStatusCode
				});
			}

			DiscordClient? discord = DiscordConnection.Client;
			
			if (discord == null) { throw new Exception("No connection to Discord"); }

			DSharpPlus.Entities.DiscordGuild guild = await discord.GetGuildAsync((ulong)guildID);

			IList<DSharpPlus.Entities.DiscordChannel> channels = new List<DSharpPlus.Entities.DiscordChannel>();


			DSharpPlus.Entities.DiscordChannel channel = guild.GetChannel((ulong)ChannelID);

			DSharpPlus.Entities.DiscordEmbedBuilder embedBuilder = new()
			{
				Title = title,
				Description = description,
			};

			
			if (embedBuilder.GetType().GetProperties()
					.Where(c => c.GetValue(embedBuilder) is string)
					.Select(c => (string?)c.GetValue(embedBuilder))
					.All(c => string.IsNullOrEmpty(c)))

			{ await discord.SendMessageAsync(channel, message); }

			else { await discord.SendMessageAsync(channel, message, embed: embedBuilder); }

			return View();
		}
	}
}