using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 来源储存库
/// </summary>
internal sealed class SourceRepository(ImageHubDbContext dbContext) :
    RepositoryBase<ImageHubDbContext, Source, SourceId>(dbContext), ISourceRepository
{

}