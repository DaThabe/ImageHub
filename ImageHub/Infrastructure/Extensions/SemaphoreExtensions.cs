namespace ImageHub.Infrastructure.Extensions;

internal static class SemaphoreExtensions
{
    extension(SemaphoreSlim semaphore)
    {
        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new Releaser(semaphore);
        }
    }

    private readonly struct Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
