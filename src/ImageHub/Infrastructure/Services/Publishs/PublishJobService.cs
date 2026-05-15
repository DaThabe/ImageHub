using ImageHub.Application.Services;
using ImageHub.Domain.Repositories;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Services.Publishs;

/// <summary>
/// 发布任务
/// </summary>
public sealed class PublishJobService(
    IPublishJobRepository publishJobRepository,
    IUnitOfWork unitOfWork
    ) : IPublishJobService
{
    public async ValueTask MarkCompletedAsync(PublishJobId id, CancellationToken cancellationToken = default)
    {
        var publish_job = await publishJobRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("未查询到资源");

        // 标记完成
        publish_job.Completed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask MarkCompletedAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default)
    {
        var publish_jobs = await publishJobRepository.FindByIdsAsync(ids, cancellationToken)
            ?? throw new InvalidOperationException("未查询到资源");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 批量完成
            foreach (var i in publish_jobs) i.Completed();

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
