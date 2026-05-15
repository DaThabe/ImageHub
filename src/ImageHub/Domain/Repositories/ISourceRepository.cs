using ImageHub.Domain.Entities;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 来源储存库
/// </summary>
public interface ISourceRepository : IRepository<Source, SourceId>;