﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TMS.APIs
{
    [Route("api/[controller]")]
    public class AccountRatesController : Controller
    {
        private IAppDateTimeService _appDateTimeService;
        public ApplicationDbContext Database { get; }
        //The following constructor code pattern is required for every Web API
        //controller class.
        public AccountRatesController(IAppDateTimeService appDateTimeService, ApplicationDbContext database)
        {
            Database = database;
            _appDateTimeService = appDateTimeService;
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id, [FromQuery]QueryPagingParametersForAccountRates inParameters)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).SingleOrDefault(c => c.CustomerAccountId == id);
                if (ca == null)
                {
                    return NotFound(new { message = "Customer could not be found" });
                }
                int pageSize = 10;
                int currentPage = 0;
                string sortOrder = "ASC";

                int totalPage = 0;
                int startRecord = 0;
                int endRecord = 0;
                int totalRecords = 0;
                if (ModelState.IsValid)
                {
                    currentPage = Int32.Parse(inParameters.page_number.ToString());
                    pageSize = Int32.Parse(inParameters.per_page.ToString());
                    sortOrder = inParameters.sort_order;
                }
                else
                {
                    currentPage = 1;
                    pageSize = 10;
                    sortOrder = "ASC";
                }
                if (currentPage == 1)
                {
                    startRecord = 1;
                }
                else
                {
                    startRecord = ((currentPage - 1) * pageSize) + 1;
                }

                List<AccountRate> cusAccountRate;
                if (sortOrder.Equals("ASC"))
                {
                    cusAccountRate = Database.AccountRates.Where(car => car.CustomerAccountId == id).OrderBy(car => car.AccountRateId).ToList();
                }
                else if (sortOrder.Equals("DESC"))
                {
                    cusAccountRate = Database.AccountRates.Where(car => car.CustomerAccountId == id).OrderByDescending(car => car.AccountRateId).ToList();
                }
                else
                {
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = cusAccountRate.Count;

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize);

                if(currentPage == totalPage)
                {
                    endRecord = startRecord + (totalRecords % pageSize) - 1;
                }
                else
                {
                    endRecord = startRecord + pageSize - 1;
                }
                List<object> cusAccountRateByPage = new List<object>();
                for (int i = startRecord-1; i <= endRecord-1; i++)
                {
                    cusAccountRateByPage.Add(new {
                        no = i,
                        id = cusAccountRate[i].AccountRateId,
                        rate = cusAccountRate[i].RatePerHour,
                        effectiveStartDate = cusAccountRate[i].EffectiveStartDate,
                        effectiveEndDate = cusAccountRate[i].EffectiveEndDate
                    });
                }

                   
                object result = new
                {
                    totalRecordCount = totalRecords,
                    totalCurrentPgRec = cusAccountRateByPage.Count,
                    currentPage = currentPage,
                    totalPage = totalPage,
                    records = cusAccountRateByPage,
                    from = startRecord,
                    to = endRecord

                };
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException.Message });
            }
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

    public class QueryPagingParametersForAccountRates
    {
        [BindRequired]
        public int page_number { get; set; }
        public int per_page { get; set; }
        public string sort_order { get; set; }

    }//end of QueryPagingParametersForAccountRates class
}
