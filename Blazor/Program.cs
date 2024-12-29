using Blazor.Components;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Blazor;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddScoped<DBService>(sp => new DBService(builder.Configuration.GetConnectionString("OwnerConnection")));

        var dbService = builder.Services.BuildServiceProvider().GetService<DBService>();
        if (!dbService.TestConnection())
        {
            Console.WriteLine("Kunne ikke oprette forbindelse til databasen");
        }
        else
        {
            Console.WriteLine("Forbindelse til database oprettet succesfuldt");
        }

        // Tilføj JWTService
        builder.Services.AddScoped<JWTService>();

        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddAuthenticationCore();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "Cookies";
        })
        .AddCookie("Cookies");

        builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

        builder.Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("DevOnly", policy => policy.RequireRole("Dev"));
            options.AddPolicy("AdminOrDev", policy => policy.RequireRole("Admin", "Dev"));
        });

        // Opsæt tabellerne (Kun ved første kørsel)
        dbService.ExecuteSqlFileAsync("SQL-Scripts/User.sql");

        builder.Services.AddBlazorBootstrap();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Run();
    }
}
