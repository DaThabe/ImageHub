using ImageHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImageHub.Infrastructure.Database;


/// <summary>
/// 数据库上下文
/// </summary>
/// <param name="options"></param>
internal class ImageHubDbContext(DbContextOptions<ImageHubDbContext> options) : DbContext(options)
{
    /// <summary>
    /// 任务
    /// </summary>
    public DbSet<Job> Jobs { get; private set; }

    /// <summary>
    /// 来源
    /// </summary>
    public DbSet<Source> Sources { get; private set; }

    /// <summary>
    /// 元信息
    /// </summary>
    public DbSet<Metadata> Metadatas { get; private set; }

    /// <summary>
    /// 资源
    /// </summary>
    public DbSet<Resource> Resources { get; private set; }

    /// <summary>
    /// 发布任务
    /// </summary>
    public DbSet<PublishJob> PublishJobs { get; private set; }

    /// <summary>
    /// 发布目标
    /// </summary>
    public DbSet<PublishTarget> PublishTargets { get; private set; }


    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ImageHubDbContext).Assembly);
    }
}

/// <summary>
/// 设计时构建工厂
/// </summary>
internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ImageHubDbContext>
{
    public ImageHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ImageHubDbContext>();
        optionsBuilder.UseSqlite("DataSource=:memory:");

        return new ImageHubDbContext(optionsBuilder.Options);
    }
}