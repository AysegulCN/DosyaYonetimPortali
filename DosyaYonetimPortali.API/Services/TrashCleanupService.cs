using DosyaYonetimPortali.API.Data;
using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortali.API.Services
{
    public class TrashCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public TrashCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var _userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                    var users = await _userManager.Users.ToListAsync(stoppingToken);

                    foreach (var user in users)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        int retentionDays = roles.Contains("PremiumUser") ? 180 : 30;
                        var deleteThreshold = DateTime.Now.AddDays(-retentionDays);

                        
                        var oldFiles = await _context.Files
                            .Where(f => f.AppUserId == user.Id && f.IsDeleted && f.UploadDate < deleteThreshold)
                            .ToListAsync(stoppingToken); 

                        foreach (var file in oldFiles)
                        {
                            if (System.IO.File.Exists(file.PhysicalPath))
                            {
                                System.IO.File.Delete(file.PhysicalPath);
                            }

                            _context.Files.Remove(file);

                            user.UsedStorage -= file.Size;
                            if (user.UsedStorage < 0) user.UsedStorage = 0; 
                        }

                        await _userManager.UpdateAsync(user);
                    }

                    await _context.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}