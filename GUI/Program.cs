using BLL.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddBusinessLayer(builder.Configuration);



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = ".PRN222_RAG.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

builder.Services.AddAuthorization();

var app = builder.Build();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();



app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Auth", action = "Login" });

app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new { controller = "Auth", action = "Register" });

app.MapControllerRoute(
    name: "logout",
    pattern: "logout",
    defaults: new { controller = "Auth", action = "Logout" });

app.MapControllerRoute(
    name: "upload",
    pattern: "upload",
    defaults: new { controller = "Documents", action = "Create" });

app.MapControllerRoute(
    name: "document-details",
    pattern: "document/{id:guid}",
    defaults: new { controller = "Documents", action = "Details" });

app.MapControllerRoute(
    name: "chat",
    pattern: "chat",
    defaults: new { controller = "Chat", action = "Index" });

app.MapControllerRoute(
    name: "admin-users",
    pattern: "admin/users",
    defaults: new { controller = "Admin", action = "Users" });

app.MapControllerRoute(
    name: "admin-approve",
    pattern: "admin/users/approve/{id:guid}",
    defaults: new { controller = "Admin", action = "Approve" });

app.MapControllerRoute(
    name: "admin-reject-block",
    pattern: "admin/users/reject-block/{id:guid}",
    defaults: new { controller = "Admin", action = "RejectOrBlock" });

app.MapControllerRoute(
    name: "admin-unblock",
    pattern: "admin/users/unblock/{id:guid}",
    defaults: new { controller = "Admin", action = "Unblock" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();