using System.Linq.Expressions;
using Users.DAL.Models;

namespace Users.DL.Services
{
    public interface IBaseUserService
    {
        Task<int> AddAsync(BaseUser entry);
        Task<IEnumerable<BaseUser>> GetAsync();
        Task<BaseUser> GetById(int id);
        Task<int> DeleteAsync(int id);
        IEnumerable<BaseUser> Where(Expression<Func<BaseUser, bool>> exp);
    }
}
