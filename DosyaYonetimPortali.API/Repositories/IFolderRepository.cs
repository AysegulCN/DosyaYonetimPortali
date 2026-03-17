using DosyaYonetimPortali.API.Models;

namespace DosyaYonetimPortali.API.Repositories
{
    // Generic yapıdan miras alıyoruz
    public interface IFolderRepository : IGenericRepository<Folder>
    {
        // İleride "Klasörü içindeki dosyalarla beraber getir" gibi özel metotlar buraya yazılır.
    }
}