using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DosyaYonetimPortali.MVC.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Index";
        options.Cookie.Name = "CoreDrive.Auth";
    });

var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
    catch { }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();