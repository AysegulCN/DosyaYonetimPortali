using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DosyaYonetimPortali.MVC.Data;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();



builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Index";
        options.Cookie.Name = "CoreDrive.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("tr-TR") };

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    try
    {
        context.Database.Migrate();

        if (!context.Users.Any())
        {
            context.Users.Add(new DosyaYonetimPortali.MVC.Models.Entities.User
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                FirstName = "Ayşegül",
                LastName = "Çoban",
                Email = "aysegulcoban@gmail.com",
                Password = "aysegul123",
                Role = "Admin",
                ProfilePictureUrl = "https://ui-avatars.com/api/?name=Admin&background=4e73df&color=fff&rounded=true"
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();