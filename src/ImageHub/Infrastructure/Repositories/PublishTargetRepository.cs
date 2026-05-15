using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;

/// <summary>
/// 发布目标储存库
/// </summary>
internal sealed class PublishTargetRepository(ImageHubDbContext dbContext) :
    RepositoryBase<ImageHubDbContext, PublishTarget, PublishTargetId>(dbContext), IPublishTargetRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async ValueTask<TelegramGroupPublishTarget?> FindByTelegramGroupIdAsync(long groupId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PublishTargets
            .OfType<TelegramGroupPublishTarget>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId, cancellationToken: cancellationToken);
    }
}