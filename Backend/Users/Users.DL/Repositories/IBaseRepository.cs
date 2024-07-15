using System.Linq.Expressions;
using Users.DAL.Context;
using Users.DAL.Models;

namespace Users.DL.Repositories
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        DataContext GetContext();
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(long id);
        IQueryable<T> Where(Expression<Func<T, bool>> exp);
        Task<T?> SingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> exp);
        Task<int> AddAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<int> DeleteRangeAsync(List<T> entities);
    }
}
