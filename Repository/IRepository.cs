using BE_API.Entites;

namespace BE_API.Repository
{
    public interface IRepository<T> where T : class, IEntity
    {
        IQueryable<T> Query();
        Task AddAsync(T entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

        void Update(T entity);

        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);
    }
}
