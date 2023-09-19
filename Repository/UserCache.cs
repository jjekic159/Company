using Company.Models;
using Repository.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Repository
{
    public class UserCache : IUserCache
    {
        private ConcurrentDictionary<string, User> _cache;
        private CancellationTokenSource _cts;

        public UserCache()
        {
            _cache = new ConcurrentDictionary<string, User>(1, 1);
            _cts = new CancellationTokenSource();

            ParallelOptions paralellOptions = new ParallelOptions()
            {
                CancellationToken = _cts.Token
            };
        }

        public void Dispose()
        {
            _cts.Dispose();
            _cts = null;
        }

        public async ValueTask<string> GetDataAsync(int companyId, int skip, int take)
        {
            string group = string.Concat(companyId, "-");
            if (!_cache.Keys.Contains(group))
            {
                await AddToCacheAsync(companyId);
            }

            return await GetDataFromCacheAsync(group, skip, take);
        }

        public async ValueTask<User> GetDataFromCacheAsync(string group, User user)
        {
            User[] output = null;
            User result = null;

            var semaphore = new SemaphoreSlim(10);
            try
            {
                result = _cache.AsParallel()
                               .Where(x => x.Key.StartsWith(group))
                               .OrderBy(x => x.Value.FirstName)
                               .Select(x => x.Value)
                               .Where(x => x.CompanyId == user.CompanyId &&
                                      x.FirstName == user.FirstName &&
                                      x.LastName == user.LastName &&
                                      x.Email == user.Email).FirstOrDefault();
            }
            finally
            {
                semaphore.Release();
            }

            await Task.Delay(1);
            return result;
        }

        public async ValueTask<string> GetDataFromCacheAsync(string group, int skip, int take)
        {
            User[] arrayOfUsers = null;
            string json = string.Empty;

            var semaphore = new SemaphoreSlim(10);
            try
            {
                await semaphore.WaitAsync();
                var output = _cache.AsParallel()
                                   .Where(x => x.Key.StartsWith(group))
                                   .OrderBy(x => x.Value.FirstName)
                                   .Select(x => x.Value)
                                   .ToArray();
                take = Math.Min(take, output.Length - skip);
                arrayOfUsers = new User[take];
                Array.ConstrainedCopy(output, skip, arrayOfUsers, 0, take);
                json = string.Empty;
            }
            finally
            {
                semaphore.Release();
            }

            using (var stream = new System.IO.MemoryStream())
            {
                await JsonSerializer.SerializeAsync(stream, arrayOfUsers, arrayOfUsers.GetType());
                stream.Position = 0;
                using var reader = new System.IO.StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
        }

        public async ValueTask<bool> RemoveFromCacheAsync(int companyId)
        {
            bool result = true;
            string group = string.Empty;
            List<IGrouping<bool, KeyValuePair<string, User>>> list = null;

            var semaphore = new SemaphoreSlim(10);
            try
            {
                await semaphore.WaitAsync();
                group = string.Concat(companyId, "-");
                list = _cache.GroupBy(x => x.Key.StartsWith(group)).ToList();
            }
            finally
            {
                semaphore.Release();
            }                        

            if (list.Count() == 0)
            {
                return result;
            }

            string[] output = null;

            semaphore = new SemaphoreSlim(10);
            try
            {
                await semaphore.WaitAsync();
                output = list[0].ToList().Select(x => x.Key).ToArray();
            }
            finally
            {
                semaphore.Release();
            }

            Parallel.For(0, output.Length, (i, paralellOptions) =>
            {
                User user = new User();
                if (_cache.TryRemove(output[i], out user))
                {
                    result = false;
                    paralellOptions.Break();
                }
            });            

            return result;
        }

        public async ValueTask<int> AddToCacheAsync(int companyId)
        {
            List<User> list = null;
            User[] users = null;
            User mock = new User();

            var semaphore = new SemaphoreSlim(10);
            try
            {
                await semaphore.WaitAsync();
                list = await AdoSqlUser.GetUsersAsync(companyId, AdoSqlUser.ConnStringStat);
                users = list.ToArray();
                Parallel.ForEach(users, user =>
                {
                    _cache.AddOrUpdate(string.Concat(user.CompanyId, "-", user.Id), user, (key, oldvalue) => user);
                });
            }
            finally
            {
                semaphore.Release();
            }            

            await Task.Delay(1);
            return 1;
        }

    }
}
