using System;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    internal class ComposedAwaitableConstraint : IAwaitableConstraint
    {
        private readonly IAwaitableConstraint _AwaitableConstraint1;
        private readonly IAwaitableConstraint _AwaitableConstraint2;
        private readonly SemaphoreSlim _Semaphore = new SemaphoreSlim(1, 1);

        internal ComposedAwaitableConstraint(IAwaitableConstraint awaitableConstraint1, IAwaitableConstraint awaitableConstraint2)
        {
            _AwaitableConstraint1 = awaitableConstraint1;
            _AwaitableConstraint2 = awaitableConstraint2;
        }

        public IAwaitableConstraint Clone()
        {
            return new ComposedAwaitableConstraint(_AwaitableConstraint1.Clone(), _AwaitableConstraint2.Clone());
        }

        public async Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken)
        {
            await _Semaphore.WaitAsync(cancellationToken);
            IDisposable[] disposables;
            try 
            {
                disposables = await Task.WhenAll(_AwaitableConstraint1.WaitForReadiness(cancellationToken), _AwaitableConstraint2.WaitForReadiness(cancellationToken));
            }
            catch (Exception) 
            {
                _Semaphore.Release();
                throw;
            } 
            return new DisposeAction(() => 
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                _Semaphore.Release();
            });
        }
    }
}
