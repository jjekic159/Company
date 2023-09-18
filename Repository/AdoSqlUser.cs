using Company.Models;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Repository
{
    public class AdoSqlUser: IUser
    {
        private IUserCache _cache;

        public static string ConnStringStat { get; set; }

        public string ConnectionString { get; set; }

        public AdoSqlUser()
        {
            _cache = new UserCache();
        }

        public async ValueTask<User> CreateUser(User user)
        {
            User addedUser = null;

            var semaphore = new SemaphoreSlim(10);
            try
            {
                await semaphore.WaitAsync();
                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    SqlCommand command = new SqlCommand("AddUser", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@firstName", SqlDbType.NVarChar).Value = user.FirstName;
                    command.Parameters.AddWithValue("@lastName", SqlDbType.NVarChar).Value = user.LastName;
                    command.Parameters.AddWithValue("@email", SqlDbType.NVarChar).Value = user.Email;
                    command.Parameters.AddWithValue("@companyId", SqlDbType.Int).Value = user.CompanyId;

                    command.Connection.Open();
                    await command.ExecuteNonQueryAsync();
                }

                var companyId = user.CompanyId;
                if (!await _cache.RemoveFromCacheAsync(companyId))
                {
                    return new User() { Email = "Error", CompanyId = -1 };
                }

                await _cache.AddToCacheAsync(companyId);

                string group = string.Concat(companyId, "-");

                addedUser = await _cache.GetDataFromCacheAsync(group, user);
            }
            finally
            {
                semaphore.Release();
            }

            return addedUser;
        }        

        public async ValueTask<string> GetUsersAsync(int companyId, int skip, int take)
        {
            return await _cache.GetDataAsync(companyId, skip, take);
        }        

        public static async ValueTask<List<User>> GetUsersAsync(int companyId, string connectionString)
        {
            List<User> list = null;

            var semaphore = new SemaphoreSlim(10);
            try
            {
                list = new List<User>();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("GetCompanyUsers", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@companyId", SqlDbType.Int).Value = companyId;
                    command.Connection.Open();
                    var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);

                    int i = 0;
                    if (reader.HasRows)
                    {                    
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var user = new User() { Id = 0, FirstName = string.Empty, LastName = string.Empty, Email = string.Empty, CompanyId = 0 };
                            if (!reader.IsDBNull(0))
                                user.Id = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                                user.FirstName = reader.GetString(1);
                            if (!reader.IsDBNull(2))
                                user.LastName = reader.GetString(2);
                            if (!reader.IsDBNull(3))
                                user.Email = reader.GetString(3);
                            if (!reader.IsDBNull(4))
                                user.CompanyId = reader.GetInt32(4);                        
                            list.Add(user);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            await Task.Delay(1);
            return list;
        }

    }
}
