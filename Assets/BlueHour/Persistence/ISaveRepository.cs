using System.Threading;
using System.Threading.Tasks;

namespace BlueHour.Persistence
{
    public interface ISaveRepository<T> where T : class
    {
        // Returns null for absent data. Read and write failures can surface to the caller.
        Task<T> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(T data, CancellationToken cancellationToken);
    }
}
