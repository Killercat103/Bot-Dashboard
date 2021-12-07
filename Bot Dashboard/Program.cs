using Bot_Dashboard;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

Startup? startup = new(builder.Configuration);

BotStartup.ConnectToDiscord().GetAwaiter().GetResult();

startup.WebHost(builder.WebHost);

startup.ConfigureServices(builder.Services);

WebApplication? app = builder.Build();

startup.Configure(app, app.Environment);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.Use(async (context, next) =>
{
	await next();

	switch (context.Response.StatusCode)
	{
		case 404:
			string url = context.Request.Path;
			context.Request.Path = "/Exception/URL404";
			context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?url=\"" + url + "\"");
			await next();
			break;
	}
});
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "dashboard",
	pattern: "/Dashboard/{guildID}/{action=Index}",
	defaults: new { controller = "Dashboard", action = "Index", }
);

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}",
	defaults: new { controller = "Home", action = "Index", }
);

app.Run();
