namespace GameServer.Shared.Database.Repository.Writer;

public interface IWriterRepository<TEntity>
    where TEntity : class
{
    Task<TEntity> AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    Task UpdateAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    Task DeleteAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}