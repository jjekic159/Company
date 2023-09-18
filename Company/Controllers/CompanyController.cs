using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository;
using Repository.Interfaces;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace Company.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompanyController : ControllerBase
    {
        private IUser dbUser;

        public CompanyController(IUser user, IConfiguration configuration)
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
        //[ValidateAntiForgeryToken]
        public async ValueTask<Models.User> Create(int companyId, [FromBody]Models.User user)//int id, IFormCollection collection)
        {
            user.CompanyId = companyId;
            return await dbUser.CreateUser(user);
        }

        // POST: CompanyController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: CompanyController/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}
        //
        //// POST: CompanyController/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
