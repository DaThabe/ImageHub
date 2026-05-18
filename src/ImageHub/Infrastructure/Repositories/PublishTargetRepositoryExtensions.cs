using ImageHub.Domain.Entities;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 发布目标储存库
/// </summary>
public static class PublishTargetRepositoryExtensions
{
    extension(IRepository<PublishTarget, PublishTargetId> repository)
    {
        public Task<TelegramGroupPublishTarget?> FindByTelegramGroupIdAsync(long groupId, CancellationToken cancellationToken = default)
        {
            return repository.Query
                .OfType<TelegramGroupPublishTarget>()
                .FirstOrDefaultAsync(x => x.GroupId == groupId, cancellationToken: cancellationToken);
        }
    }
}