using DosyaYonetimPortali.API.Data;
using DosyaYonetimPortali.API.Models;

namespace DosyaYonetimPortali.API.Repositories
{
    public class FileRepository : GenericRepository<AppFile>, IFileRepository
    {
        public FileRepository(AppDbContext context) : base(context)
        {
        }
    }
}