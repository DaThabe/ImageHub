using ImageHub.Enums;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ImageHub.Infrastructure.Services.Sources;


/// <summary>
/// 来源信号量
/// </summary>
public interface ISourceSemaphoreSlim
{
    void UpdateConcurrency(SourceType sourceType, int maxConcurrent);
    Task WaitAsync(SourceType sourceType, CancellationToken cancellationToken = default);
    void Release(SourceType sourceType);
}

public sealed class SourceSemaphoreSlim : ISourceSemaphoreSlim
{
    private readonly ConcurrentDictionary<SourceType, SemaphoreSlim> _semaphores = [];
    private readonly IOptionsMonitor<SourceConcurrencyOptions> _options;

    public SourceSemaphoreSlim(IOptionsMonitor<SourceConcurrencyOptions> options)
    {
        _options = options;

        _options.OnChange((newSettings, _) =>
        {
            foreach (var kv in newSettings.Concurrency) UpdateConcurrency(kv.Key, kv.Value);
        });
    }

    public void UpdateConcurrency(SourceType sourceType, int maxConcurrent)
    {
        if (_semaphores.TryGetValue(sourceType, out var oldSemaphore))
        {
            var newSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            var currentCount = oldSemaphore.CurrentCount;
            for (int i = 0; i < currentCount; i++)
            {
                newSemaphore.Wait();  // 消耗新信号量
            }

            _semaphores[sourceType] = newSemaphore;
            oldSemaphore.Dispose();
        }
        else
        {
            _semaphores[sourceType] = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        }
    }

    public async Task WaitAsync(SourceType sourceType, CancellationToken ct = default)
    {
        var maxConcurrent = _options.CurrentValue.Concurrency.GetValueOrDefault(sourceType, _options.CurrentValue.DefaultMaxConcurrency);
        var semaphore = _semaphores.GetOrAdd(sourceType, _ => new SemaphoreSlim(maxConcurrent, maxConcurrent));
        await semaphore.WaitAsync(ct);
    }

    public void Release(SourceType sourceType)
    {
        if (_semaphores.TryGetValue(sourceType, out var semaphore))
        {
            semaphore.Release();
        }
    }
}