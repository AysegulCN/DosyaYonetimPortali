using DosyaYonetimPortali.API.Data;
using DosyaYonetimPortali.API.Data;
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

                    // 30 günden eski olan silinmiş dosyaları bul
                    var deleteThreshold = DateTime.Now.AddDays(-30);
                    var oldFiles = await _context.Files
                        .Where(f => f.IsDeleted && f.UploadDate < deleteThreshold)
                        .ToListAsync();

                    foreach (var file in oldFiles)
                    {
                        if (System.IO.File.Exists(file.PhysicalPath))
                            System.IO.File.Delete(file.PhysicalPath);

                        _context.Files.Remove(file);
                    }
                    await _context.SaveChangesAsync();
                }
                // Robot günde bir kez kontrol eder
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}