using ImageHub.Application.Services;
using ImageHub.Commands;
using ImageHub.Domain.Entities;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;
using ThabeSoft.Mediator;

namespace ImageHub.Application.Handlers;


/// <summary>
/// 创建任务命令
/// </summary>
internal sealed class CreateJobCommandHandler(
    ISourceParser sourceService,
    IRepository<Source, SourceId> sourceRepository,
    IRepository<Job, JobId> jobRepository,
    IUnitOfWork unitOfWork
    ) : IRequestHandler<CreateJobCommand, CreateJobResult>
{
    public async ValueTask<CreateJobResult> HandleAsync(CreateJobCommand createJob, CancellationToken cancellationToken = default)
    {
        // 获取来源
        var source = sourceService.Parse(createJob.Url);

        if (!await sourceRepository.Query.AnyAsync(x => x.Id == source.Id, cancellationToken))
        {
            await sourceRepository.AddAsync(source, cancellationToken);
        }

        // 创建任务
        var job = await jobRepository.Query.FirstOrDefaultAsync(x => x.SourceId == source.Id, cancellationToken);
        if(job is null)
        {
            job = new Job(JobId.Create(), source.Id);
            await jobRepository.AddAsync(job, cancellationToken);
        }

        // 保存修改
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateJobResult(job.Id);
    }
}