using System.Threading.Tasks;

namespace ComposableAsync
{
    /// <summary>
    ///  Asynchronous version of IDisposable
    /// For reference see discussion: https://github.com/dotnet/roslyn/issues/114
    /// </summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        ///  Performs asynchronously application-defined tasks associated with freeing,
        ///  releasing, or resetting unmanaged resources.
        /// </summary>
        Task DisposeAsync();
    }
}
