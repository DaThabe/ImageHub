namespace ImageHub.Application;


/// <summary>
/// 用于管理数据库事务和批量提交
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellation = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}


/// <summary>
/// 事务
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// 提交修改
    /// </summary>
    /// <returns></returns>
    Task CommitAsync();
    /// <summary>
    /// 回滚修改
    /// </summary>
    /// <returns></returns>
    Task RollbackAsync();
}