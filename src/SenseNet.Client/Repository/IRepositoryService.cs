using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace SenseNet.Client
{
    public interface IRepositoryService
    {
        public Task<IRepository> GetRepositoryAsync(CancellationToken cancel);
        public Task<IRepository> GetRepositoryAsync(string name, CancellationToken cancel);
    }
}
