using DSharpPlus;
using Tommy;

namespace Bot_Dashboard
{
	public class HostConfig
	{
		public static string? LocalURL { get; set; }
		public static string? HostName { get; set; }

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

			/* I'm gonna need this later probably.
			DSharpPlus.Entities.DiscordGuild guild = await discord.GetGuildAsync(878193265424343060L);
			DSharpPlus.Entities.DiscordChannel channel = guild.GetChannel(878340382319079475L);

			await discord.SendMessageAsync(channel, "Hello world!"); */
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
				do
				{
					input = Console.ReadLine();
				}
				while (String.IsNullOrEmpty(input));
				return input;
			}

			static string EncryptString(string value)
			{
				char[] valueCharArray = value.ToCharArray();
				string encryptedString = "";
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
				["HostURL"] = RequestConfigParameter("\nPlease insert application's host URL. (Example: https://0.0.0.0:443;http://0.0.0.0:80)"),
				["Discord"] =
					{
						["ClientID"] = RequestConfigParameter("\nPlease insert Discord application's client ID"),
						["EncryptedBotToken"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord Bot's token")),
						["EncryptedClientSecret"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord application's client secret"))
					},
			};

			newHostConfig.WriteTo(writer);
			writer.Flush();
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
				decryptedBotToken = "";
			char[] botTokenCharArray = botToken.ToCharArray();
			
			// Insecure deencryption method.
			for (int i = 0; i < botTokenCharArray.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					--botTokenCharArray[i];
				decryptedBotToken += botTokenCharArray[i];
			}

			string clientSecret = hostConfig["Discord"]["EncryptedClientSecret"],
				decryptedClientSecret = "";
			char[] clientSecretCharArray = clientSecret.ToCharArray();


			for (int i = 0; i < clientSecretCharArray.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					--clientSecretCharArray[i];
				decryptedClientSecret += clientSecretCharArray[i];
			}

			IList<long> admins = new List<long>();
			for (int i = 0; i < hostConfig["Discord"]["Administrators"].ChildrenCount; ++i)
			{
				admins.Add(hostConfig["Discord"]["Administrators"][i]);
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

		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddRazorPages();

			services.AddSession(options =>
			{
				options.Cookie.Name = "Discord.Session";
				options.IdleTimeout = TimeSpan.FromMinutes(60);
				options.Cookie.HttpOnly = true;
			});
		}

		public static void WebHost(ConfigureWebHostBuilder builder)
		{
			if (HostConfig.LocalURL != null) { builder.UseUrls(HostConfig.LocalURL); }
		}

		public static void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
		{
			app.UseSession();
			app.UseAuthentication();
		}
	}
}