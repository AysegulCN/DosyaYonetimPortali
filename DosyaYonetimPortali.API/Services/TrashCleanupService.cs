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

                    // Tüm kullanıcıları çekiyoruz ki her birinin Premium olup olmadığına bakalım
                    var users = await _userManager.Users.ToListAsync(stoppingToken);

                    foreach (var user in users)
                    {
                        // 1. KULLANICI TİPİNE GÖRE SÜRE BELİRLEME
                        var roles = await _userManager.GetRolesAsync(user);
                        int retentionDays = roles.Contains("PremiumUser") ? 180 : 30;
                        var deleteThreshold = DateTime.Now.AddDays(-retentionDays);

                        // 2. KULLANICIYA AİT SİLİNMESİ GEREKEN DOSYALARI BUL
                        // Not: Eğer AppFile modelinde DeletedDate yoksa, mutlaka eklemelisin.
                        // Çöp kutusuna ne zaman atıldığını bilmeliyiz ki 30 gün sayabilelim.
                        var oldFiles = await _context.Files
                            .Where(f => f.AppUserId == user.Id && f.IsDeleted && f.UploadDate < deleteThreshold)
                            .ToListAsync(stoppingToken); // İleride UploadDate yerine DeletedDate kullanman kusursuz olur.

                        foreach (var file in oldFiles)
                        {
                            // A) Fiziksel Dosyayı Sunucudan Sil
                            if (System.IO.File.Exists(file.PhysicalPath))
                            {
                                System.IO.File.Delete(file.PhysicalPath);
                            }

                            // B) Veritabanından Kaldır
                            _context.Files.Remove(file);

                            // C) KOTAYI İADE ET (Kullanıcının alanını geri ver)
                            user.UsedStorage -= file.Size;
                            if (user.UsedStorage < 0) user.UsedStorage = 0; // Güvenlik önlemi
                        }

                        // Kullanıcının iade edilen kotasını kaydet
                        await _userManager.UpdateAsync(user);
                    }

                    // Tüm veritabanı silme işlemlerini onayla
                    await _context.SaveChangesAsync(stoppingToken);
                }

                // Robot günde bir kez kontrol eder
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}