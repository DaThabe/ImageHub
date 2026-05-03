using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 储存库基类
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TId"></typeparam>
/// <param name="dbContext"></param>
internal abstract class RepositoryBase<TEntity, TId>(ImageHubDbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    public async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.AddAsync(entity, cancellationToken);
    }

    public async ValueTask<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
    }

    public async ValueTask<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id.Equals(id), cancellationToken);
    }

    public async ValueTask UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(entity);

        // 如果实体未被跟踪，先附加
        if (entry.State == EntityState.Detached)
        {
            var trackedEntity = dbContext.Set<TEntity>().Local
               .FirstOrDefault(e => Equals(e.Id, entity.Id));

            if (trackedEntity is not null)
            {
                // 已被跟踪，用跟踪的实例更新
                dbContext.Entry(trackedEntity).CurrentValues.SetValues(entity);
                entry = dbContext.Entry(trackedEntity);
            }
            else
            {
                // 未跟踪，正常附加
                dbContext.Set<TEntity>().Attach(entity);
                entry = dbContext.Entry(entity);
            }
        }

        entry.State = EntityState.Modified;
    }


    public async ValueTask<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        //TODO: 危险操作, 数据量大会卡

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
    public async ValueTask<IReadOnlyCollection<TEntity>> GetPagedAsync(int take, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (take <= 0) throw new ArgumentException("take 必须大于 0", nameof(take));
        if (skip < 0) throw new ArgumentException("skip 不能为负数", nameof(skip));

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
    public async ValueTask<IReadOnlyCollection<TEntity>> GetAllAfterSkipAsync(int skip, CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentException("skip 不能为负数", nameof(skip));

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .ToListAsync(cancellationToken);
    }


    public async ValueTask<IReadOnlyCollection<TResult>> GetAllAsync<TResult>(CancellationToken cancellationToken = default) where TResult : TEntity
    {
        //TODO: 危险操作, 数据量大会卡

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OfType<TResult>()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
    public async ValueTask<IReadOnlyCollection<TResult>> GetPagedAsync<TResult>(int take, int skip = 0, CancellationToken cancellationToken = default) where TResult : TEntity
    {
        if (take <= 0) throw new ArgumentException("take 必须大于 0", nameof(take));
        if (skip < 0) throw new ArgumentException("skip 不能为负数", nameof(skip));

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OfType<TResult>()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
    public async ValueTask<IReadOnlyCollection<TResult>> GetAllAfterSkipAsync<TResult>(int skip, CancellationToken cancellationToken = default) where TResult : TEntity
    {
        if (skip < 0) throw new ArgumentException("skip 不能为负数", nameof(skip));

        return await dbContext.Set<TEntity>()
            .AsNoTracking()
            .OfType<TResult>()
            .OrderBy(x => x.Id)
            .Skip(skip)
            .ToListAsync(cancellationToken);
    }
}