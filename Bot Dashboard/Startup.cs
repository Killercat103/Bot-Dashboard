using DSharpPlus;
using Tommy;

namespace Bot_Dashboard
{
	public static class HostConfig
	{

		public static string LocalURL { get; set; } = "https://127.0.0.1:5000";
		public static string HostName { get; set; } = "Bot Dashboard";

		public static class Discord
		{
			public static string? ClientID { get; set; }
			public static string? BotToken { get; set; }
			public static string? ClientSecret { get; set; }
			public static long[]? Admins { get; set; }
		}
	}

	public class DiscordConnection
	{
		public static DiscordClient? Client { get; set; }
	}

	// Connecting to the Discord Bot.
	public class BotStartup
	{
		public static async Task ConnectToDiscord()
		{
			DiscordClient? discord = new(new DiscordConfiguration()
			{
				Token = HostConfig.Discord.BotToken,
				TokenType = TokenType.Bot
			});

			await discord.ConnectAsync();

			DiscordConnection.Client = discord;
		}
	}


	// Startup class needed to connect to the Discord bot
	class Startup
	{
		readonly public TomlTable hostConfig;

		// Function to create a TOML configuration file if the latter one was invalid.
		private static TomlTable CreateNewConfig()
		{
			static string RequestConfigParameter(string message)
			{
				Console.WriteLine(message);
				string? input;

				do { input = Console.ReadLine(); }

				while (string.IsNullOrEmpty(input));
				
				return input;
			}

			static string EncryptString(string value)
			{
				char[] valueCharArray = value.ToCharArray();
				string encryptedString = string.Empty;
				// Insecure encryption method.
				for (int i = 0; i < valueCharArray.Length; ++i)
				{
					for (int j = 1; j <= i; ++j)
						++valueCharArray[i];
					encryptedString += valueCharArray[i];
				}
				return encryptedString;
			}
			if (RequestConfigParameter("\nWould you like to create a new configuration file? (Y/N)").ToLower() == "n")
				Environment.Exit(-1);

			using StreamWriter writer = File.CreateText("Config.toml");
			TomlTable newHostConfig = new()
			{
				["HostName"] = RequestConfigParameter("\nPlease insert application's name."),
				["LocalURL"] = RequestConfigParameter("\nPlease insert application's locally hosted URL. (Example: https://0.0.0.0:443;http://0.0.0.0:80)"),
				["Discord"] =
					{
						["ClientID"] = Convert.ToInt64(RequestConfigParameter("\nPlease insert Discord application's client ID")),
						["EncryptedBotToken"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord Bot's token")),
						["EncryptedClientSecret"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord application's client secret")),
						["Administrators"] = Array.Empty<TomlNode>()
					},
			};

			newHostConfig.WriteTo(writer);
			writer.Flush();
			Console.WriteLine("Created \"Config.toml\"!\n" +
				"Some more options are available for manual edit. (Remember to delete the file if something goes wrong).");
			return newHostConfig;
		}

		public Startup(IConfiguration configuration)
		{
			try
			{
				using StreamReader reader = File.OpenText("Config.toml");
				hostConfig = TOML.Parse(reader);

				if (hostConfig["Discord"].Keys.ToArray().Length < 3)
					throw new TomlSyntaxException("Missing parameters...", TOMLParser.ParseState.Table, 1, 1);
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Configuration file not found...");
				hostConfig = CreateNewConfig();
			}
			catch (TomlSyntaxException)
			{
				Console.WriteLine("Configuration file missing arguments or improperly formated...");
				hostConfig = CreateNewConfig();
			}

			string botToken = hostConfig["Discord"]["EncryptedBotToken"],
				decryptedBotToken = string.Empty;
			char[] botTokenCharArray = botToken.ToCharArray();
			
			// Insecure decryption method.
			for (int i = 0; i < botTokenCharArray.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					--botTokenCharArray[i];
				decryptedBotToken += botTokenCharArray[i];
			}

			string clientSecret = hostConfig["Discord"]["EncryptedClientSecret"],
				decryptedClientSecret = string.Empty;
			char[] clientSecretCharArray = clientSecret.ToCharArray();


			for (int i = 0; i < clientSecretCharArray.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					--clientSecretCharArray[i];
				decryptedClientSecret += clientSecretCharArray[i];
			}

			IList<long> admins = new List<long>();
			foreach (TomlNode node in hostConfig["Discord"]["Administrators"])
			{
				admins.Add(node);
			}

			HostConfig.LocalURL = hostConfig["LocalURL"];
			HostConfig.HostName = hostConfig["HostName"];
			HostConfig.Discord.ClientID = hostConfig["Discord"]["ClientID"];
			HostConfig.Discord.BotToken = decryptedBotToken;
			HostConfig.Discord.ClientSecret = decryptedClientSecret;
			HostConfig.Discord.Admins = admins.ToArray();

			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddRazorPages();

			services.AddSession(options =>
			{
				options.Cookie.Name = "Discord.Session";
				options.IdleTimeout = TimeSpan.FromMinutes(60);
				options.Cookie.HttpOnly = true;
				options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			});
		}

		public void WebHost(ConfigureWebHostBuilder builder)
		{
			if (HostConfig.LocalURL != null) { builder.UseUrls(HostConfig.LocalURL); }
		}


		public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
		{
			app.UseSession();
			app.UseAuthentication();
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
				Path.Combine(Directory.GetCurrentDirectory())),
				RequestPath = "/wwwroot"
			});

			environment.ApplicationName = HostConfig.HostName;
		}
	}
}