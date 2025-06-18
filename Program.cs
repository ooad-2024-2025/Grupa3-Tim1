using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Identity.UI;
using Matchletic.Services;
using Matchletic.Middlewares;


var builder = WebApplication.CreateBuilder(args);

// Dodajte konfiguraciju logginga ovdje
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // For Razor Pages support
builder.Services.AddScoped<Matchletic.Services.UserSyncService>();
// U Program.cs, nakon builder.Services.AddScoped<Matchletic.Services.UserSyncService>();
builder.Services.AddScoped<Matchletic.Services.UserRegistrationEventHandler>();
builder.Services.ConfigureIdentityEvents(); // Dodaje konfiguraciju Identity događaja
// In Program.cs - add this line before building the app
builder.Services.AddScoped<NotifikacijaService>();
builder.Services.AddScoped<MecRequestService>();
builder.Services.AddScoped<PostignuceService>();




// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services with custom paths
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    // Password settings if needed
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
// Removed AddDefaultUI() since you don't have the package

// Configure the Identity login path
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Identity/Account/Login"; // Changed to use your existing controller
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Matchletic.Services.EmailSender>();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Nakon što je app izgrađena, a prije app.Run()
// Inicijalizacija baze podataka i seed-anje dostignuća
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();  // Primijeni migracije ako postoje
        await dbContext.SeedPostignucaAsync();    // Seed dostignuća
        logger.LogInformation("Seeded the database.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication(); // This must come before UseAuthorization
app.UseAuthorization();

// Add session middleware
app.UseSession();

app.UseSessionSync();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // For Razor Pages routes

app.Run();

// Program.cs ili Startup.cs
app.UseAuthentication();
app.UseAuthorization();

// Dodajte middleware za provjeru autentikacije prije MapControllerRoute
app.Use(async (context, next) =>
{
    // Provjera je li korisnik autentificiran
    if (!context.User.Identity.IsAuthenticated
        && !context.Request.Path.StartsWithSegments("/Identity")
        && !context.Request.Path.StartsWithSegments("/lib")
        && !context.Request.Path.StartsWithSegments("/css")
        && !context.Request.Path.StartsWithSegments("/js")
        && !context.Request.Path.StartsWithSegments("/imgs"))
    {
        context.Response.Redirect("/Identity/Account/Login");
        return;
    }

    await next.Invoke();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

