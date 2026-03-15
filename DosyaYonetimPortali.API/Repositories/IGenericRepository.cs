using System.Linq.Expressions;

namespace DosyaYonetimPortali.API.Repositories
{
    // <T> ifadesi bunun "Joker" bir sınıf olduğunu gösterir. T yerine Folder da gelebilir, AppFile da.
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        // Özel sorgular için (Örn: Sadece belirli klasördeki dosyaları getir)
        Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveAsync();
    }
}