using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Repository;
using Repository.Interfaces;
using System.Threading.Tasks;

namespace Company.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompanyController : ControllerBase
    {
        private IAdoSqlUser dbUser;

        public CompanyController(IAdoSqlUser user, IConfiguration configuration)
        {
            this.dbUser = user;
            this.dbUser.ConnectionString = configuration.GetConnectionString("MyConnection");
            AdoSqlUser.ConnStringStat = this.dbUser.ConnectionString;
        }

        [Route("{companyId}/Users/{from}/{take}")]
        [HttpGet]
        public async ValueTask<string> Users(int companyId, int from=1, int take=5)
        {
            return await dbUser.GetUsersAsync(companyId, from, take);            
        }

        [Route("{companyId}")]
        [HttpPost]
        public async ValueTask<Models.User> Create(int companyId, [FromBody]Models.User user)
        {
            user.CompanyId = companyId;
            return await dbUser.CreateUser(user);
        }
        
    }
}
