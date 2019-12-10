using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TMS.APIs
{
    [Route("api/[controller]")]
    public class CustomerAccountsController : Controller
    {

        private IAppDateTimeService _appDateTimeService;
        public ApplicationDbContext Database { get; }
        //The following constructor code pattern is required for every Web API
        //controller class.
        public CustomerAccountsController(IAppDateTimeService appDateTimeService, ApplicationDbContext database)
        {
            Database = database;
            _appDateTimeService = appDateTimeService;
        }

        // GET: api/<controller>
        [HttpGet]
        public IActionResult Get()
        {
            List<CustomerAccount> cusAccQueryList = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).ToList();
            List<Object> cusAccList = new List<object>();

            foreach(CustomerAccount ca in cusAccQueryList){
                cusAccList.Add(new {
                    id = ca.CustomerAccountId,
                    accountName = ca.AccountName,
                    //comments = ca.Comments.Count,
                    comments = Database.CustomerAccountComments.Where(c => c.UpdatedAt.CompareTo(_appDateTimeService.GetCurrentDateTime().AddDays(-3)) > 0).Count(c => c.CustomerAccountId == ca.CustomerAccountId),
                    visibility = ca.IsVisible,
                    createdBy = ca.CreatedBy.FullName,
                    updatedBy = ca.UpdatedBy.FullName,
                    updatedAt = ca.UpdatedAt
                });
            }

            return Ok(cusAccList);
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
