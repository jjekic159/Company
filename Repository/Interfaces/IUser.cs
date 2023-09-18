using Company.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUser
    {
        ValueTask<User> CreateUser(User user);

        ValueTask<string> GetUsersAsync(int companyId, int skip, int take);

        //ValueTask<List<User>> GetUsersAsync(int companyId);        

        string ConnectionString { get; set; }
    }
}
