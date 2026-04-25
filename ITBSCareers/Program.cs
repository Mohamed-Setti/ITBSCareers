using ITBSCareers.Models.Carriere;
using ITBSCareers.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

builder.Services.AddDbContext<CarriereDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("CarriereCS")
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

await EnsureJobOfferColumnsAsync(app);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");


app.Run();

static async Task EnsureJobOfferColumnsAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CarriereDbContext>();

        var sql = @"
IF COL_LENGTH('dbo.JobOffers', 'RequiredDegree') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredDegree NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.JobOffers', 'RequiredLevel') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredLevel NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.JobOffers', 'RequiredField') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredField NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.JobOffers', 'RequiredSkillsCsv') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredSkillsCsv NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.JobOffers', 'RequiredInterestsCsv') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredInterestsCsv NVARCHAR(MAX) NULL;";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
    catch
    {
        // keep startup resilient if database is unavailable
    }
}
