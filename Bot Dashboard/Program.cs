using Bot_Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

Startup? startup = new(builder.Configuration);

BotStartup.ConnectToDiscord().GetAwaiter().GetResult();

global::Bot_Dashboard.Startup.WebHost(builder.WebHost);

global::Bot_Dashboard.Startup.ConfigureServices(builder.Services);

WebApplication? app = builder.Build();

global::Bot_Dashboard.Startup.Configure(app, app.Environment);

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
	if (context.Response.StatusCode == 404)
	{
		string url = context.Request.Path;
		context.Request.Path = "/Exception/URL404";
		context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?url=\"" + url + "\"");
		await next();
	}
});
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
