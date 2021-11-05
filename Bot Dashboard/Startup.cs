using DSharpPlus;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using Tommy;

namespace Bot_Dashboard
{
	// Connecting to the Discord Bot.
	public partial class BotStartup
	{
		// Function to create a TOML configuration file if the latter one was invalid.
		private static string CreateNewConfig()
		{
			string? botToken;
			do
			{
				Console.WriteLine("Please provide Discord bot token.");
				botToken = Console.ReadLine();
			}
			while
				(string.IsNullOrEmpty(botToken));

			char[] botTokenChar = botToken.ToCharArray();
			string encryptedBotToken = "";

			// Insecure encryption method.
			for (int i = 0; i < botTokenChar.Length; ++i)
			{
				for (int j = 1; j <= i; ++j)
					++botTokenChar[i];
				encryptedBotToken += botTokenChar[i];
			}

			using (StreamWriter writer = File.CreateText("Config.toml"))
			{
				TomlTable table = new()
				{

					["EncryptedBotToken"] = new TomlString
					{
						Value = encryptedBotToken,
						Comment = "Token affiliated with the hosted Discord bot."
					},
				};
				table.WriteTo(writer);
				writer.Flush();
			}

			return botToken;
		}

		public static async Task MainAsync()
		{
			// Attempts to read the TOML configuration file.
			string botToken = "";
			try
			{
				using StreamReader reader = File.OpenText("Config.toml");
				TomlTable table = TOML.Parse(reader);

				botToken = table["EncryptedBotToken"];

				if (string.IsNullOrEmpty(botToken) || botToken == "Tommy.TomlLazy")
				{
					throw new TomlSyntaxException("Token not found", TOMLParser.ParseState.Table, 1, 1);
				}

				// Insecure decryption method.
				char[] botTokenChar = botToken.ToCharArray();
				botToken = "";

				for (int i = 0; i < botTokenChar.Length; ++i)
				{
					for (int j = 1; j <= i; ++j)
						--botTokenChar[i];
					botToken += botTokenChar[i];
				}
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Configuration file not found...");
				botToken = CreateNewConfig();
			}
			catch (TomlSyntaxException)
			{
				Console.WriteLine("Configuration file missing arguments or improperly formated...");
				botToken = CreateNewConfig();
			}

			DiscordClient? discord = new(new DiscordConfiguration()
			{
				Token = botToken,
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
		public Startup(IConfiguration configuration)
		{
			BotStartup.MainAsync().GetAwaiter().GetResult();

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
					ValidIssuer = Configuration.GetValue<string>("Jwt:Issuer"),
					ValidAudience = Configuration.GetValue<string>("Jwt:Audience"),
					IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("Jwt:EncryptionKey")))
				};
			})
			.AddOAuth("Discord",
				options => 
				{
					options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
					options.Scope.Add("identify");
					options.Scope.Add("guilds");

					options.CallbackPath = new PathString("/Auth/OAuthCallback");

					options.ClientId = Configuration.GetValue<string>("Discord:ClientID");
					options.ClientSecret = Configuration.GetValue<string>("Discord:ClientSecret");

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