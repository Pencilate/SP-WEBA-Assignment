using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [Authorize("ADMIN")]
        [HttpGet]
        public IActionResult Get()
        {
            List<Object> cusAccList = new List<object>();
            try
            {
                List<CustomerAccount> cusAccQueryList = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).ToList();


                foreach (CustomerAccount ca in cusAccQueryList)
                {
                    cusAccList.Add(new
                    {
                        id = ca.CustomerAccountId,
                        accountName = ca.AccountName,
                        //comments = ca.Comments.Count,
                        comments = Database.CustomerAccountComments.Where(c => c.UpdatedAt.CompareTo(_appDateTimeService.GetCurrentDateTime().Date.AddDays(-3)) > 0).Count(c => c.CustomerAccountId == ca.CustomerAccountId),
                        visibility = ca.IsVisible,
                        createdBy = ca.CreatedBy.FullName,
                        updatedBy = ca.UpdatedBy.FullName,
                        updatedAt = ca.UpdatedAt
                    });
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
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
        [Authorize("ADMIN")]
        [HttpPost("Create")]
        public IActionResult Post([FromForm] IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value);
            
                CustomerAccount ca = new CustomerAccount()
                {
                    Comments = new List<CustomerAccountComment>(),
                };
            try
            {
                ca.AccountName = data["accountName"];
                ca.IsVisible = Boolean.Parse(data["visibility"]);
                ca.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                ca.CreatedById = userId;
                ca.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                ca.UpdatedById = userId;

                if (!String.IsNullOrEmpty(data["accountComments"]))
                {
                    CustomerAccountComment cac = new CustomerAccountComment();
                    cac.Comment = data["accountComments"].ToString().Trim();
                    cac.CustomerAccountId = ca.CustomerAccountId;
                    cac.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                    cac.CreatedById = userId;
                    cac.ParentId = null;
                    cac.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                    ca.Comments.Add(cac);
                }

                AccountRate ar = new AccountRate();
                ar.CustomerAccountId = ca.CustomerAccountId;
                ar.RatePerHour = decimal.Parse(data["accountRate"]);
                ar.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                ar.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                ar.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                ar.CreatedById = userId;
                ar.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                ar.UpdatedById = userId;
                ca.AccountRates.Add(ar);


                Database.CustomerAccounts.Add(ca);
                Database.SaveChanges();

            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException.Message });
            }
            return Ok(new { message = "Successfully Registered " + ca.AccountName });

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
