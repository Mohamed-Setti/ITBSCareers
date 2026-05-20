using ITBSCareers.Models.Carriere;
using ITBSCareers.Hubs;
using ITBSCareers.Security;
using ITBSCareers.Services.Forum;
using ITBSCareers.Services.Messaging;
using ITBSCareers.Services.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 64 * 1024;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.AccessDeniedPath = "/User/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VerifiedAlumni", policy =>
        policy.RequireAuthenticatedUser()
              .Requirements.Add(new VerifiedAlumniRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, VerifiedAlumniHandler>();
builder.Services.AddScoped<IForumRepository, ForumRepository>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddSingleton<MessagingPresenceTracker>();
builder.Services.AddScoped<IPrivateMessagingRepository, PrivateMessagingRepository>();
builder.Services.AddScoped<IPrivateMessagingService, PrivateMessagingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddDbContext<CarriereDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CarriereCS")));

var app = builder.Build();

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

await EnsureForumSchemaAsync(app);
await EnsurePrivateMessagingSchemaAsync(app);
await EnsureAlumniContactVisibilitySchemaAsync(app);
await SeedForumCategoriesAsync(app);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();

static async Task EnsureForumSchemaAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CarriereDbContext>();

        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "create_forum_schema.sql");
        if (!File.Exists(scriptPath))
        {
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath);
        var batches = sql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await context.Database.ExecuteSqlRawAsync(batch);
        }
    }
    catch (SqlException)
    {
        // keep startup resilient
    }
}

static async Task EnsureAlumniContactVisibilitySchemaAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CarriereDbContext>();

        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "add_alumni_contact_visibility.sql");
        if (!File.Exists(scriptPath))
        {
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath);
        var batches = sql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await context.Database.ExecuteSqlRawAsync(batch);
        }
    }
    catch (SqlException)
    {
        // keep startup resilient
    }
}

static async Task EnsurePrivateMessagingSchemaAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CarriereDbContext>();

        var scriptPath = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "create_private_messaging_schema.sql");
        if (!File.Exists(scriptPath))
        {
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath);
        var batches = sql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await context.Database.ExecuteSqlRawAsync(batch);
        }
    }
    catch (SqlException)
    {
        // keep startup resilient
    }
}

static async Task SeedForumCategoriesAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CarriereDbContext>();

        if (await context.ForumCategories.AnyAsync())
        {
            return;
        }

        context.ForumCategories.AddRange(
            new ForumCategory { Name = "Orientation carrière", Description = "Questions sur les parcours, stages et premiers emplois.", IsActive = true },
            new ForumCategory { Name = "Compétences techniques", Description = "Discussions autour du développement, data, cloud et outils.", IsActive = true },
            new ForumCategory { Name = "Entretiens", Description = "Astuces pour les entretiens et retours d'expérience.", IsActive = true },
            new ForumCategory { Name = "Vie professionnelle", Description = "Culture d'entreprise, soft skills et évolution de carrière.", IsActive = true },
            new ForumCategory { Name = "Responsabilité numérique", Description = "Veille sur les bonnes pratiques et la sécurité.", IsActive = true }
        );

        await context.SaveChangesAsync();
    }
    catch
    {
        // keep startup resilient
    }
}
