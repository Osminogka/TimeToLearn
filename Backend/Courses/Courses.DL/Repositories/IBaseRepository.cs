using Courses.DAL.Models;

namespace Courses.DL.Repositories
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<int> AddAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<int> DeleteRangeAsync(List<T> entities);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(long id);
        IQueryable<T> Where(System.Linq.Expressions.Expression<Func<T, bool>> exp);
        Task<T?> SingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> exp);
    }
}
