using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    /// Simplified version of <see cref="IDispatcher"/> that can be converted
    /// to a <see cref="IDispatcher"/> using the ToFullDispatcher extension method
    /// </summary>
    public interface IBasicDispatcher
    {
        /// <summary>
        /// Clone dispatcher 
        /// </summary>
        /// <returns></returns>
        IBasicDispatcher Clone();

        /// <summary>
        /// Enqueue the function and return a task corresponding
        /// to the execution of the task
        /// /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> Enqueue<T>(Func<T> action, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueue the action and return a task corresponding
        /// to the execution of the task
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Enqueue(Action action, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueue the task and return a task corresponding
        /// to the execution of the task
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Enqueue(Func<Task> action, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueue the task and return a task corresponding
        /// to the execution of the original task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> Enqueue<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    }
}
