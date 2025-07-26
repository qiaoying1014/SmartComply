using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using Microsoft.AspNetCore.Identity;
using SmartComply.Models;
using Rotativa.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// --------------------------------------------------------------------
// Replace your existing AddDbContext(...) with this block:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOpts =>
        {
          // Automatically retry on transient failures:
          npgsqlOpts.EnableRetryOnFailure(
              maxRetryCount: 5,
              maxRetryDelay: TimeSpan.FromSeconds(10),
              errorCodesToAdd: null);

          // Give commands (FindAsync/SaveChangesAsync) up to 60s before timing out
          npgsqlOpts.CommandTimeout(60);
        }
    )
);
// --------------------------------------------------------------------

// Configure session
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30); // Adjust as needed
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews().AddSessionStateTempDataProvider();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.LoginPath = "/Auth/Login";
      options.LogoutPath = "/Auth/Logout";
      options.AccessDeniedPath = "/Auth/AccessDenied";
      options.ExpireTimeSpan = TimeSpan.FromMinutes(500); // Cookie expiration time
      options.SlidingExpiration = true; // Sliding expiration
    });

builder.Services.AddAuthorization();

// Register the IPasswordHasher service
builder.Services.AddSingleton<IPasswordHasher<Staff>, PasswordHasher<Staff>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

var env = app.Environment;
RotativaConfiguration.Setup(env.WebRootPath, "Rotativa");

app.Run();
