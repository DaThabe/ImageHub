using ImageHub.Entities;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using ImageHub.Repositories;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 来源储存库
/// </summary>
internal sealed class SourceRepository(ImageHubDbContext dbContext) :
    EfCoreRepositoryBase<ImageHubDbContext, Source, SourceId>(dbContext), ISourceRepository
{

}