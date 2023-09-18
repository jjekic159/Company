using Company.Models;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserCache
    {
        ValueTask<string> GetDataAsync(int id, int skip, int take);
        ValueTask<User> GetDataFromCacheAsync(string group, User user);
        ValueTask<string> GetDataFromCacheAsync(string group, int skip, int take);
        ValueTask<bool> RemoveFromCacheAsync(int id);
        ValueTask<int> AddToCacheAsync(int companyId);
    }
}
