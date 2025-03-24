using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface IRepository<TEntity> where TEntity : IEntity
    {
        Task<List<TEntity>> FindAllAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task<TEntity> FindByIdAsync(int id);
    }

    public interface IEntity
    {
        int Id { get; set; }
    }
}