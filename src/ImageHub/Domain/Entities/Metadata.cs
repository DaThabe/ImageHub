using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 图像元数据
/// </summary>
public sealed class Metadata : AggregateRoot<MetadataId>
{
    private readonly List<string> _resources;
    private readonly List<string> _tags = [];


    /// <summary>
    /// 来源 Id
    /// </summary>
    public SourceId SourceId { get; }
    /// <summary>
    /// 资源链接
    /// </summary>
    public IReadOnlyCollection<string> Resources => _resources.ToHashSet();

    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; private set; }
    /// <summary>
    /// 作者名称
    /// </summary>
    public string? AuthorName { get; private set; }
    /// <summary>
    /// 作者主页
    /// </summary>
    public string? AuthorUrl { get; private set; }
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; private set; }
    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTimeOffset? UploadAt { get; private set; }
    /// <summary>
    /// 标签
    /// </summary>
    public IReadOnlyCollection<string> Tags => _tags.ToHashSet();


    private Metadata()
    {
        _resources = [];
    }
    public Metadata(MetadataId id, SourceId sourceId, IEnumerable<string> resources) : base(id)
    {
        if (resources?.Any() != true)
        {
            throw new ArgumentException("至少需要一个资源", nameof(resources));
        }

        SourceId = sourceId;
        _resources = [.. resources];
    }



    public Metadata ChangeTitle(string? title)
    {
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        return this;
    }


    public Metadata ChangeAuthorName(string? name)
    {
        AuthorName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        return this;
    }
    public Metadata ChangeAuthorUrl(string? url)
    {
        AuthorUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        return this;
    }
    public Metadata ChangeAuthor(string? name, string? url = null)
    {
        ChangeAuthorName(name);
        ChangeAuthorUrl(url);

        return this;
    }


    public Metadata ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        return this;
    }

    public Metadata ChangeUploadTime(DateTimeOffset? uploadAt)
    {
        UploadAt = uploadAt <= DateTimeOffset.MinValue ? null : uploadAt;
        return this;
    }


    public Metadata AddResource(IEnumerable<string> resources)
    {
        foreach (var resource in resources) AddResource(resource);
        return this;
    }
    public Metadata AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag))
        {
            _tags.Add(tag.Trim());
        }

        return this;
    }
    public Metadata AddTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags) AddTag(tag);
        return this;
    }

    public Metadata AddResource(string resource)
    {
        if (!string.IsNullOrWhiteSpace(resource) && !_resources.Contains(resource))
        {
            _resources.Add(resource.Trim());
        }

        return this;
    }
    public Metadata AddResources(IEnumerable<string> resources)
    {
        foreach (var resource in resources) AddResource(resource);
        return this;
    }
}