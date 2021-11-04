using DSharpPlus;
using Tommy;


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
			(String.IsNullOrEmpty(botToken));

		char[] botTokenChar = botToken.ToCharArray();
		string encryptedBotToken = "";

		// Insecure encryption method.
		for (int i = 0; i < botTokenChar.Length; ++i)
		{
			for (int j = 1; j <= i; ++j)
				++botTokenChar[i];
			encryptedBotToken += botTokenChar[i];
		}

		using (StreamWriter writer = File.CreateText("Host_Configuration/Config.toml"))
		{
			TomlTable table = new()
			{
				["EncryptedBotToken"] = encryptedBotToken
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
			using StreamReader reader = File.OpenText("Host_Configuration/Config.toml");
			TomlTable table = TOML.Parse(reader);

			botToken = table["EncryptedBotToken"];

			if (String.IsNullOrEmpty(botToken) || botToken == "Tommy.TomlLazy")
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

		var discord = new DiscordClient(new DiscordConfiguration()
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
		services.AddRazorPages();
		services.AddAuthorization();
	}


	public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
	{

	}
}