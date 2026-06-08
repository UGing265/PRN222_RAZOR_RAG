using BLL.Extensions;
using GUI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddBusinessLayer(builder.Configuration);

// HttpClient for proxying requests to Better Auth service (avoids Mixed Content on HTTPS)
var betterAuthUrl = builder.Configuration["BetterAuth:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient("BetterAuth", client =>
{
    client.BaseAddress = new Uri(betterAuthUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var documentService = scope.ServiceProvider.GetRequiredService<BLL.Interfaces.Documents.IDocumentService>();
    try
    {
        await documentService.SeedInitialDataAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapChatEndpoints();
app.MapAuthProxyEndpoints();

app.Run();

