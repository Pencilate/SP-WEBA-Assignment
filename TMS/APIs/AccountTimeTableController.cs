using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    public class AccountTimeTableController : Controller
    {
        private IAppDateTimeService _appDateTimeService;
        public ApplicationDbContext Database { get; }
        //The following constructor code pattern is required for every Web API
        //controller class.
        public AccountTimeTableController(IAppDateTimeService appDateTimeService, ApplicationDbContext database)
        {
            Database = database;
            _appDateTimeService = appDateTimeService;
        }

        [Authorize("ADMIN")]
        [HttpGet("GetAccountTimeTablePaginated/{cusID}/{arID}")]
        public IActionResult Get(int cusID, int arID, [FromQuery]QueryPagingParametersForAccountTimeTable inParameters)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).SingleOrDefault(c => c.CustomerAccountId == cusID); //Check if the specific CustomerAccount exist in the database
                if (ca == null) //If CustomerAccount does not exist in database, return 404 Not Found
                {
                    return NotFound(new { message = "Customer could not be found" });
                }
                AccountRate ar = Database.AccountRates.SingleOrDefault(r => r.AccountRateId == arID); //Check if the specific AccountRate exist in the database
                if (ar == null) //If CustomerAccount does not exist in database, return 404 Not Found
                {
                    return NotFound(new { message = "Customer's Account Rate could not be found" });
                }
                //Pagination parameters
                int pageSize = 10;
                int currentPage = 0;
                string sortOrder = "ASC";

                int totalPage = 0;
                int startRecord = 0;
                int endRecord = 0;
                int totalRecords = 0;
                if (ModelState.IsValid) //if query params is valid load the data into pagination parameters
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
                if (currentPage == 1)  //Calculate the page's first record's index
                {
                    startRecord = 1;
                }
                else
                {
                    startRecord = ((currentPage - 1) * pageSize) + 1;
                }

                List<AccountTimeTable> cusAccountTimeTable; //Declare a list to store all the AccountRate records related to the customer from the database
                if (sortOrder.Equals("ASC"))
                {
                    //Query the database for AccountRate records related to the customer in the ascending order of AccountRateId
                    cusAccountTimeTable = Database.AccountTimeTable.Where(catt => catt.AccountRateId == arID).OrderBy(catt => catt.AccountTimeTableId).ToList();
                }
                else if (sortOrder.Equals("DESC"))
                {
                    //Query the database for AccountRate records related to the customer in the descending order of AccountRateId
                    cusAccountTimeTable = Database.AccountTimeTable.Where(catt => catt.AccountRateId == arID).OrderByDescending(catt => catt.AccountTimeTableId).ToList();
                }
                else
                {
                    //return BadRequest for invalid sort order
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = cusAccountTimeTable.Count; //Get the total no of AccountRate records from the datebase

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate the total number of pages
                if (totalRecords == 0)
                {
                    //If no records are found for the customer
                    return Ok(new
                    {
                        accountName = ca.AccountName,
                        accountRate = ar.RatePerHour,
                        totalRecordCount = totalRecords,
                        totalCurrentPgRec = 0,
                        currentPage = 0,
                        totalPage = 0,
                        records = new List<object>(),
                        from = 0,
                        to = 0

                    }); //Return the response object
                }
                //Calculate the index of the last of the records on the current page
                if ((currentPage == totalPage) && (totalRecords % pageSize != 0))
                {
                    endRecord = startRecord + (totalRecords % pageSize) - 1;
                }
                else
                {
                    endRecord = startRecord + pageSize - 1;
                }

                //Iterate through the list of AccountRates, and cherry pick the data to return
                List<object> cusAccountRateByPage = new List<object>();
                for (int i = startRecord - 1; i <= endRecord - 1; i++)
                {
                    int recordNo = -1;
                    if (sortOrder.Equals("ASC"))
                    {
                        recordNo = i + 1;
                    }
                    else if (sortOrder.Equals("DESC"))
                    {
                        recordNo = totalRecords - i;
                    }
                    cusAccountRateByPage.Add(new
                    {
                        no = recordNo,
                        id = cusAccountTimeTable[i].AccountTimeTableId,
                        day = cusAccountTimeTable[i].DayOfWeekNumber,
                        startTime = cusAccountTimeTable[i].EffectiveStartDateTime.TimeOfDay,
                        endTime = cusAccountTimeTable[i].EffectiveEndDateTime.TimeOfDay,
                        startDate = cusAccountTimeTable[i].EffectiveStartDateTime.Date,
                        endDate = cusAccountTimeTable[i].EffectiveEndDateTime.Date,
                        visibility = cusAccountTimeTable[i].IsVisible
                    });
                }

                //Craft the response object to return   
                object result = new
                {
                    accountName = ca.AccountName,
                    accountRate = ar.RatePerHour,
                    totalRecordCount = totalRecords,
                    totalCurrentPgRec = cusAccountRateByPage.Count,
                    currentPage = currentPage,
                    totalPage = totalPage,
                    records = cusAccountRateByPage,
                    from = startRecord,
                    to = endRecord

                };
                return Ok(result); //Return the response object
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "There are issues with the request." }); //Return BadRequest for any general exception occured
            }
        }

        [Authorize("ADMIN")]
        [HttpGet("GetAccountTimeTable/{id}")]
        public IActionResult GetTimeTable(int id)
        {
            try
            {
                AccountTimeTable att = Database.AccountTimeTable.SingleOrDefault(tt => tt.AccountTimeTableId == id);
                if (att == null)
                {
                    return NotFound(new { message = "Customer account timetable could not be found" });
                }
                object result = new
                {
                    day = att.DayOfWeekNumber,
                    startTime = att.EffectiveStartDateTime.TimeOfDay,
                    endTime = att.EffectiveEndDateTime.TimeOfDay,
                    startDate = att.EffectiveStartDateTime.Date,
                    endDate = att.EffectiveEndDateTime.Date,
                    visibility = att.IsVisible
                };
                return Ok(att);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
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

    public class QueryPagingParametersForAccountTimeTable
    {
        //Query parameters for pagination
        [BindRequired]
        public int page_number { get; set; }
        public int per_page { get; set; }
        public string sort_order { get; set; }

    }//end of QueryPagingParametersForAccountRates class
}
