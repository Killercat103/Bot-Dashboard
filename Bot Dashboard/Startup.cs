using DSharpPlus;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tommy;

namespace Bot_Dashboard
{
	// Connecting to the Discord Bot.
	public partial class BotStartup
	{
		public static async Task MainAsync(TomlTable hostConfig)
		{
			string botToken = hostConfig["Discord"]["EncryptedBotToken"];
			char[] botTokenCharArray = botToken.ToCharArray();
			string encryptedBotToken = "";
			// Insecure encryption method.
			for (int i = 0; i < botTokenCharArray.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					--botTokenCharArray[i];
				encryptedBotToken += botTokenCharArray[i];
			}

			DiscordClient? discord = new(new DiscordConfiguration()
			{
				Token = encryptedBotToken,
				TokenType = TokenType.Bot
			});

			await discord.ConnectAsync();

			/* I'm gonna need this later probably.
			DSharpPlus.Entities.DiscordGuild guild = await discord.GetGuildAsync(878193265424343060L);
			DSharpPlus.Entities.DiscordChannel channel = guild.GetChannel(878340382319079475L);

			await discord.SendMessageAsync(channel, "Hello world!"); */
		}
	}


	// Startup class needed to connect to the Discord bot
	class Startup
	{
		readonly TomlTable hostConfig;

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
				["Discord"] =
					{
						["ClientID"] = RequestConfigParameter("\nPlease insert Discord application's client ID"),
						["EncryptedBotToken"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord Bot's token")),
						["EncryptedClientSecret"] =  EncryptString(RequestConfigParameter("\nPlease insert Discord application's client secret"))
					},

				["Jwt"] =
					{
						["Audience"] = RequestConfigParameter("\nPlease insert OAuth's audience URL"),
						["EncryptionKey"] = new TomlString
						{
							Value = RequestConfigParameter("\nPlease insert OAuth's Encryption Key"),
							Comment = "Random string to use as a encryption key. (Recommended to use a random password generator with 128 characters.)"
						},

						["Issuer"] = RequestConfigParameter("\nPlease insert OAuth's Issuer URL")
					}
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

				if (hostConfig["Discord"].Keys.ToArray().Length < 3 || hostConfig["Jwt"].Keys.ToArray().Length < 3)
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

			BotStartup.MainAsync(hostConfig).GetAwaiter().GetResult();

			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddRazorPages();
			services.AddAuthentication(options =>
			{
				options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
				options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
			})
			.AddCookie()
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new()
				{
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateIssuerSigningKey = true,
					ValidIssuer = hostConfig["Jwt"]["Issuer"],
					ValidAudience = hostConfig["Jwt"]["Audience"],
					IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(hostConfig["Jwt"]["EncryptionKey"]))
				};
			})
			.AddOAuth("Discord",
				options => 
				{
					options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
					options.Scope.Add("identify");
					options.Scope.Add("guilds");

					options.CallbackPath = new PathString("/Auth/OAuthCallback");

					options.ClientId = hostConfig["Discord"]["ClientID"];

					string clientSecret = hostConfig["Discord"]["EncryptedClientSecret"];
					char[] clientSecretCharArray = clientSecret.ToCharArray();
					string encryptedClientSecret = "";
					// Insecure encryption method.
					for (int i = 0; i < clientSecretCharArray.Length; ++i)
					{
						for (int j = 1; j <= i; ++j)
							--clientSecretCharArray[i];
						encryptedClientSecret += clientSecretCharArray[i];
					}


					options.ClientSecret = encryptedClientSecret;

					options.TokenEndpoint = "https://discord.com/api/oauth2/token";
					options.UserInformationEndpoint = "https://discord.com/api/users/@me";

					options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
					options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");

					options.AccessDeniedPath = "/Users/DiscordAuthFailed";

					options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
					{
						OnCreatingTicket = async context =>
						{
							HttpRequestMessage request = new(HttpMethod.Get, context.Options.UserInformationEndpoint);
							request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
							request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
						
							HttpResponseMessage? response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
							response.EnsureSuccessStatusCode();

							System.Text.Json.JsonElement user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

							context.RunClaimActions(user);
						}
					};
				}
			);
		}


		public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
		{
			app.UseAuthentication();
		}
	}
}