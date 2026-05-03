using ImageHub.Domain.Entities;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 储存库通用接口
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TId"></typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : IEntity<TId>
    where TId : IEquatable<TId>
{
    ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);


    ValueTask<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyCollection<TEntity>> GetPagedAsync(int take, int skip = 0, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyCollection<TEntity>> GetAllAfterSkipAsync(int skip, CancellationToken cancellationToken = default);


    ValueTask<IReadOnlyCollection<TResult>> GetAllAsync<TResult>(CancellationToken cancellationToken = default) where TResult : TEntity;
    ValueTask<IReadOnlyCollection<TResult>> GetPagedAsync<TResult>(int take, int skip = 0, CancellationToken cancellationToken = default) where TResult : TEntity;
    ValueTask<IReadOnlyCollection<TResult>> GetAllAfterSkipAsync<TResult>(int skip, CancellationToken cancellationToken = default) where TResult : TEntity;
}