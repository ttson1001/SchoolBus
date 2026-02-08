using BE_API.Database;
using BE_API.Entites;
using Microsoft.EntityFrameworkCore;

namespace BE_API.Repository
{
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        protected readonly DbSet<T> _set;

        public Repository(BeContext context)
        {
            _set = context.Set<T>();
        }

        public IQueryable<T> Query()
            => _set.AsQueryable();

        public async Task AddAsync(T entity, CancellationToken ct = default)
            => await _set.AddAsync(entity, ct);

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
            => await _set.AddRangeAsync(entities, ct);

        public void Update(T entity)
            => _set.Update(entity);

        public void Delete(T entity)
            => _set.Remove(entity);

        public void DeleteRange(IEnumerable<T> entities)
            => _set.RemoveRange(entities);
    }

}
