using ImageHub.Application.Services;
using ImageHub.Domain.Entities;
using ImageHub.Models;
using ImageHub.Repositories;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Services.Publishs;


internal sealed class PublishTargetService(
    IPublishTargetRepository publishTargetRepository,
    IUnitOfWork unitOfWork
    ) : IPublishTargetService
{
    public async ValueTask SetTelegramGroup(long groupId, CancellationToken cancellationToken = default)
    {
        var target = await publishTargetRepository.FindByTelegramGroupIdAsync(groupId, cancellationToken);
        if (target is not null) return;

        target = new TelegramGroupPublishTarget(PublishTargetId.Create(), groupId);
        await publishTargetRepository.AddAsync(target, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}