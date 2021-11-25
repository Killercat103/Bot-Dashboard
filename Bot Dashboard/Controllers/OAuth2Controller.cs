using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace Bot_Dashboard.Controllers
{

	public class OAuth2Controller : Controller
	{
		protected class ClientUser
		{
			public string? Access_Token { get; set; }
			public long ID { get; set; }
			public string? Username { get; set; }
			public string? Discriminator { get; set; }
			public string? Avatar { get; set; }
		}

		public IActionResult Login()
		{

			return Redirect("https://discord.com/api/v9/oauth2/authorize?" +
				"response_type=code&" +
				"client_id=" + HostConfig.Discord.ClientID + "&" +
				"scope=identify%20guilds&" +
				"state=user&" +
				"redirect_uri=https://" + HttpContext.Request.Host.ToString() + "/OAuth2/Callback&" +
				"prompt=consent"
			);
		}

		public IActionResult Callback(string code)
		{
			RestClient restClient = new();
			RestRequest restRequest = new();

			if (HostConfig.Discord.ClientID == null) { throw new Exception("Host configuration missing value!"); }
			restRequest.AddParameter("client_id", HostConfig.Discord.ClientID);
			restRequest.AddParameter("grant_type", "authorization_code");
			restRequest.AddParameter("code", code);
			restRequest.AddParameter("redirect_uri", "https://" + HttpContext.Request.Host.ToString() + "/OAuth2/Callback");
			restRequest.AddParameter("scope", "identify guilds");

			if (HostConfig.Discord.ClientSecret == null) { throw new Exception("Host configuration missing value!"); }
	
			restRequest.AddParameter("client_secret", HostConfig.Discord.ClientSecret);

			restRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
			
			restClient.BaseUrl = new Uri("https://discord.com/api/v9/oauth2/token");
			IRestResponse? response = restClient.Post(restRequest);



			if (response.Content == null) { return RedirectToAction("DiscordAuthFailed", "Exception"); }

			ClientUser? clientUser = JsonConvert.DeserializeObject<ClientUser>(response.Content);
			if (clientUser?.Access_Token == null) { return RedirectToAction("DiscordAuthFailed", "Exception"); }

			string access_Token = clientUser.Access_Token;

			restClient = new();
			restRequest = new();


			restClient.BaseUrl = new Uri("https://discord.com/api/v9/users/@me");

			restRequest.AddHeader("Authorization",
				"Bearer " + access_Token);

			response = restClient.Get(restRequest);


			if (response.StatusCode != System.Net.HttpStatusCode.OK) { return RedirectToAction("DiscordAuthFailed", "Exception"); }
			
			clientUser = JsonConvert.DeserializeObject<ClientUser>(response.Content);


			if (clientUser == null) { return RedirectToAction("DiscordAuthFailed", "Exception"); }
			clientUser.Access_Token = access_Token;


			string clientSessionKey = JsonConvert.SerializeObject(clientUser);

			HttpContext.Session.SetString("Discord.Session", clientSessionKey);
			return RedirectToAction("Guilds", "Users");
		}
	}
}
