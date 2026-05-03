using ImageHub.Application;
using ImageHub.Domain.Events;
using ImageHub.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 用于管理数据库事务和批量提交
/// </summary>
/// <param name="dbContext"></param>
/// <param name="loggerFactory"></param>
internal sealed class UnitOfWork(
    ImageHubDbContext dbContext, 
    IDomainEventPublisher domainEventDispatcher, 
    ILoggerFactory loggerFactory
    ) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 获取所有包含领域事件的实体
        var has_event_entities = dbContext.ChangeTracker
            .Entries()
            .Select(x => x.Entity)
            .OfType<IDomainEventProvider>()
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        // 保存所有修改
        await dbContext.SaveChangesAsync(cancellationToken);

        // 没有事件
        if (has_event_entities.Count == 0) return;

        // 分发事件
        foreach (var @event in has_event_entities.SelectMany(x => x.DomainEvents))
        {
            await domainEventDispatcher.PublsihAsync(@event, cancellationToken);
        }

        // 清空事件
        foreach (var entity in has_event_entities.OfType<IDomainEventClearable>())
        {
            entity.ClearDomainEvents();
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var logger = loggerFactory.CreateLogger<Transaction>();

        return new Transaction(transaction, logger);
    }
}

/// <summary>
/// 用于管理数据库事务和批量提交
/// </summary>
/// <param name="transaction"></param>
/// <param name="logger"></param>
public sealed class Transaction(IDbContextTransaction transaction, ILogger<Transaction> logger) : ITransaction
{
    private bool _committed;

    public async Task CommitAsync()
    {
        await transaction.CommitAsync();
        _committed = true;
    }

    public async Task RollbackAsync()
    {
        await transaction.RollbackAsync();
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            await transaction.RollbackAsync();
            logger.LogWarning("事务未提交, 已自动回滚, Id:{id}", transaction.TransactionId);
        }

        await transaction.DisposeAsync();
    }
}