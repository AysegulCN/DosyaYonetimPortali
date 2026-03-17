using DosyaYonetimPortali.API.Controllers.Data;
using DosyaYonetimPortali.API.Models;

namespace DosyaYonetimPortali.API.Repositories
{
    public class FolderRepository : GenericRepository<Folder>, IFolderRepository
    {
        public FolderRepository(AppDbContext context) : base(context)
        {
        }
        // Özel metotların içleri (SQL sorguları) buraya yazılır.
    }
}