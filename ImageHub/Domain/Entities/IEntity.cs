namespace ImageHub.Domain.Entities;


/// <summary>
/// 实体接口
/// </summary>
/// <typeparam name="TId"></typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    TId Id { get; }
}


/// <summary>
/// 实体
/// </summary>
/// <typeparam name="TId"></typeparam>
public class Entity<TId> : IEntity<TId>
{
    public TId Id { get; }


    protected Entity()
    {
        Id = default!;
    }

    protected Entity(TId id)
    {
        Id = id;
    }
}