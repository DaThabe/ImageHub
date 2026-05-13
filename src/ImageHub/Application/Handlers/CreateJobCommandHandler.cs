using ImageHub.Application.Services;
using ImageHub.Commands;
using ImageHub.Domain.Repositories;
using ThabeSoft.DomainDrivenDesign;
using ThabeSoft.Mediator;

namespace ImageHub.Application.Handlers;


/// <summary>
/// 创建任务命令
/// </summary>
internal sealed class CreateJobCommandHandler(
    ISourceParser sourceService,
    ISourceRepository sourceRepository,
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork
    ) : IRequestHandler<CreateJobCommand, CreateJobResult>
{
    public async ValueTask<CreateJobResult> HandleAsync(CreateJobCommand createJob, CancellationToken cancellationToken = default)
    {
        // 获取来源
        var source = sourceService.Parse(createJob.Url);
        await sourceRepository.UpsertAsync(source, cancellationToken);

        // 创建任务
        var job = await jobRepository.GetOrCreateBySourceIdAsync(source.Id, cancellationToken);

        // 保存修改
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateJobResult(job.Id);
    }
}