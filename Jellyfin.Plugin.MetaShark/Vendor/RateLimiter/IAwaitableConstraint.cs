using System;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    /// <summary>
    /// Represents a time constraints that can be awaited
    /// </summary>
    public interface IAwaitableConstraint
    {
        /// <summary>
        /// returns a task that will complete once the constraint is fulfilled
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancel the wait
        /// </param>
        /// <returns>
        /// A disposable that should be disposed upon task completion
        /// </returns>
        Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken);

        /// <summary>
        /// Returns a new IAwaitableConstraint with same constraints but unused
        /// </summary>
        /// <returns></returns>
        IAwaitableConstraint Clone();
    }
}
