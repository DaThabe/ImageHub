using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;

namespace ImageHub.Infrastructure.Repositories;

/// <summary>
/// 来源储存库
/// </summary>
/// <param name="dbContext"></param>
internal sealed class SourceRepository(ImageHubDbContext dbContext) :
    RepositoryBase<Source, SourceId>(dbContext), ISourceRepository
{
}