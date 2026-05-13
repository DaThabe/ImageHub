using ImageHub.Models;
using ThabeSoft.Mediator;

namespace ImageHub.Commands;


/// <summary>
/// 创建任务命令
/// </summary>
public readonly record struct CreateJobCommand(string Url) : IRequest<CreateJobResult>;

/// <summary>
/// 创建任务结果
/// </summary>
public readonly record struct CreateJobResult(JobId JobId);