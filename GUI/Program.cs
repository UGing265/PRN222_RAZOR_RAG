using BLL.Extensions;
using BLL.Interfaces.Notifications;
using GUI.Endpoints;
using GUI.Hubs;
using GUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
builder.Services.AddBusinessLayer(builder.Configuration);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<DAL.Data.DBContext>();
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    if (user == null || !user.IsActive || user.IsBlocked)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
                else
                {
                    context.RejectPrincipal();
                }
            }
        };
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
app.MapHub<SystemHub>("/systemHub");

app.Run();

