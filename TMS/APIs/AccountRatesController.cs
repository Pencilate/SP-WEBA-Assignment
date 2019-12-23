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
        [Authorize("ADMIN")]
        [HttpGet("GetAccountRatesPaginated/{id}")]
        public IActionResult Get(int id, [FromQuery]QueryPagingParametersForAccountRates inParameters)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).SingleOrDefault(c => c.CustomerAccountId == id); //Check if the specific CustomerAccount exist in the database
                if (ca == null) //If CustomerAccount does not exist in database, return 404 Not Found
                {
                    return NotFound(new { message = "Customer could not be found" });
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

                List<AccountRate> cusAccountRate; //Declare a list to store all the AccountRate records related to the customer from the database
                if (sortOrder.Equals("ASC"))
                {
                    //Query the database for AccountRate records related to the customer in the ascending order of AccountRateId
                    cusAccountRate = Database.AccountRates.Where(car => car.CustomerAccountId == id).OrderBy(car => car.AccountRateId).ToList();
                }
                else if (sortOrder.Equals("DESC"))
                {
                    //Query the database for AccountRate records related to the customer in the descending order of AccountRateId
                    cusAccountRate = Database.AccountRates.Where(car => car.CustomerAccountId == id).OrderByDescending(car => car.AccountRateId).ToList();
                }
                else
                {
                    //return BadRequest for invalid sort order
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = cusAccountRate.Count; //Get the total no of AccountRate records from the datebase

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate the total number of pages
                if (totalRecords == 0)
                {
                    //If no records are found for the customer
                    return Ok(new
                    {
                        accountName = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id).AccountName,
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
                if((currentPage == totalPage) && (totalRecords % pageSize != 0)) 
                {
                    endRecord = startRecord + (totalRecords % pageSize) - 1;
                }
                else
                {
                    endRecord = startRecord + pageSize - 1;
                }

                //Iterate through the list of AccountRates, and cherry pick the data to return
                List<object> cusAccountRateByPage = new List<object>();
                for (int i = startRecord-1; i <= endRecord-1; i++)
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
                    cusAccountRateByPage.Add(new {
                        no = recordNo,
                        id = cusAccountRate[i].AccountRateId,
                        rate = cusAccountRate[i].RatePerHour,
                        effectiveStartDate = cusAccountRate[i].EffectiveStartDate,
                        effectiveEndDate = cusAccountRate[i].EffectiveEndDate
                    });
                }

                //Craft the response object to return   
                object result = new
                {
                    accountName = ca.AccountName,
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
        [HttpGet("{cusId}/{rateId}")]
        public IActionResult GetRate(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId); //Query the specific account rate with the specified AccountRateId
                if(ar == null) //If AccountRate record is not found return NotFound response
                {
                    return NotFound(new { message = "Customer account rate could not be found" });

                }
                //Craft response object to return
                object arObject = new
                {
                    rate = ar.RatePerHour,
                    startDate = ar.EffectiveStartDate,
                    endDate = ar.EffectiveEndDate
                };
                return Ok(arObject); //Return Ok respose with response object
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
        }

        [Authorize("ADMIN")]
        [HttpGet("RateSummary/{cusId}/{rateId}")]
        public IActionResult GetAccountRateSummary(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId); //Query database for the specifc AccountRate record
                if (ar == null) //If AccountRate record is not found return NotFound response
                {
                    return NotFound(new { message = "Customer account rate could not be found" });
                }

                int ttCount = Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).Count(); //Retrieve the number of AccountTimeTable records related to the AccountRate record

                //Craft response object and return with Ok 200 response
                object arObject = new
                {
                    rate = ar.RatePerHour,
                    startDate = ar.EffectiveStartDate,
                    endDate = ar.EffectiveEndDate,
                    ttCount = ttCount
                };
                return Ok(arObject);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });  //If any database related operation occur, return ISE
            }
        }

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("Create/{id}")]
        public IActionResult Post(int id, [FromForm]IFormCollection data)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

                AccountRate newAR = new AccountRate(); //Initialise empty AccountRate object and populate it with webFormData
                newAR.CustomerAccountId = id;
                newAR.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                newAR.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                newAR.RatePerHour = decimal.Parse(data["accountRate"]);
                newAR.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                newAR.CreatedById = userId;
                newAR.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                newAR.UpdatedById = userId;

                //Checking for overlap
                List<AccountRate> allAR = Database.AccountRates.Where(r => r.CustomerAccountId == id).ToList(); //Retrieve all AccountRate records related to the Customer
                bool overlap = false;
                List<String> overlapMsg = new List<string>();
                foreach (AccountRate ar in allAR) //Iterate thorugh all the AccountRate record to check for overlap
                {
                    bool overlapCurrentRecord = false;
                    if (newAR.EffectiveStartDate.CompareTo(ar.EffectiveStartDate) >= 0)// Check if the new AR Start Date is After the current AR Start Date
                    {
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) <= 0)// Check if the new AR End Date is Before the current AR End Date
                        {
                            overlapCurrentRecord = true; //Record overlap the entire period
                        }
                        if (newAR.EffectiveStartDate.CompareTo(ar.EffectiveEndDate) <= 0)// Check if the new AR Start Date is Before the current AR End Date
                        {
                            overlapCurrentRecord = true; // Record overlap front of period
                        }

                    }
                    else// The new AR Start Date is Before the current AR Start Date
                    {
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) >= 0)// Check if the new AR End Date is After the current AR End Date
                        {
                            overlapCurrentRecord = true; //Record is encapulated by period
                        }
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveStartDate) >= 0)// Check if new AR End Date is After the current AR Start Date
                        {
                            overlapCurrentRecord = true; //Record over back of period
                        }
                    }
                    if (overlapCurrentRecord) //If there is overlap, collate a string of all the date clashes
                    {
                        overlap = true;
                        overlapMsg.Add(ar.EffectiveStartDate.ToString("d") + " and " + ar.EffectiveEndDate.ToString("d"));
                    }
                }


                if (overlap) //If there is overlap, return a BadRequest response with the date period overlap
                {
                    String message = "Overlaps with existing rate dates! Dates cannot be between ";
                    for (int i = 0; i < overlapMsg.Count; i++) //Iterate through all overlapMsg to create a complete message
                    {
                        if (i > 0)
                        {
                            message += " or ";
                        }
                        message += overlapMsg[i] + " inclusive";
                    }
                    return BadRequest(new { message = message });
                }
                else //No overlap occurs
                {
                    foreach (AccountRate ar in allAR) //Iterate through the records to check if the date range is continuous
                    {
                        if ((newAR.RatePerHour == ar.RatePerHour) && ((newAR.EffectiveStartDate.AddDays(-1).Date.CompareTo(ar.EffectiveEndDate) == 0) || (newAR.EffectiveEndDate.AddDays(1).Date.CompareTo(ar.EffectiveStartDate) == 0))) //If new AR is continuous, return BadRequest asking for the user to extend the other record instead.
                        {
                            return BadRequest(new { message = "Extend your existing account rate! Existing account rate with the same rate/hour of $" + ar.RatePerHour + " is successive with your new account rate." });

                        }
                    }
                    
                    //If the record has no overlap and non-continuous, save the new AccountRate record to database and return a Ok 200 response.
                    Database.AccountRates.Add(newAR);
                    Database.SaveChanges();
                    return Ok(new { message = "Successfully added record." });

                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "There are issues with the request." });
            }
        }

        // PUT api/<controller>/5
        [Authorize("ADMIN")]
        [HttpPut("Update/{cusId}/{rateId}")]
        public IActionResult UpdateCustomerAccountRate(int cusId, int rateId, [FromForm]IFormCollection data)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

                AccountRate arToUpdate = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId); //Retrieve the specific AccountRate record with the specified AccountRateId
                //Update information in the retrieved AccountRate record
                arToUpdate.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                arToUpdate.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                arToUpdate.RatePerHour = decimal.Parse(data["accountRate"]);
                arToUpdate.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                arToUpdate.UpdatedById = userId;

                List<AccountRate> allAR = Database.AccountRates.Where(r => r.CustomerAccountId == cusId).Where(r => r.AccountRateId != rateId).ToList(); //Retrieve all AccountRate records related to the Customer
                bool overlap = false;
                List<String> overlapMsg = new List<string>();
                foreach (AccountRate ar in allAR) //Iterate thorugh all the AccountRate record to check for overlap
                {
                    bool overlapCurrentRecord = false;
                    if (arToUpdate.EffectiveStartDate.CompareTo(ar.EffectiveStartDate) >= 0)// Check if the new AR Start Date is After the current AR Start Date
                    {
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) <= 0)// Check if the new AR End Date is Before the current AR End Date
                        {
                            overlapCurrentRecord = true; //Record overlap the entire period
                        }
                        if (arToUpdate.EffectiveStartDate.CompareTo(ar.EffectiveEndDate) <= 0)// Check if the new AR Start Date is Before the current AR End Date
                        {
                            overlapCurrentRecord = true; // Record overlap front of period
                        }

                    }
                    else// The new AR Start Date is Before the current AR Start Date
                    {
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) >= 0)// Check if the new AR End Date is After the current AR End Date
                        {
                            overlapCurrentRecord = true; //Record is encapulated by period
                        }
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveStartDate) >= 0)// Check if new AR End Date is After the current AR Start Date
                        {
                            overlapCurrentRecord = true; //Record over back of period
                        }
                    }
                    if (overlapCurrentRecord)//If there is overlap, collate a string of all the date clashes
                    {
                        overlap = true;
                        overlapMsg.Add(ar.EffectiveStartDate.ToString("d") + " and " + ar.EffectiveEndDate.ToString("d"));
                    }
                }


                if (overlap)//If there is overlap, return a BadRequest response with the date period overlap
                {
                    String message = "Overlaps with other existing rate dates! Dates cannot be between ";
                    for (int i = 0; i < overlapMsg.Count; i++)//Iterate through all overlapMsg to create a complete message
                    {
                        if (i > 0)
                        {
                            message += " or ";
                        }
                        message += overlapMsg[i] + " inclusive";
                    }
                    return BadRequest(new { message = message });
                }
                else//No overlap occurs
                {
                    foreach (AccountRate ar in allAR)//Iterate through the records to check if the date range is continuous
                    {
                        if ((arToUpdate.RatePerHour == ar.RatePerHour) && ((arToUpdate.EffectiveStartDate.AddDays(-1).Date.CompareTo(ar.EffectiveEndDate) == 0) || (arToUpdate.EffectiveEndDate.AddDays(1).Date.CompareTo(ar.EffectiveStartDate) == 0)))//If updated AR is continuous, return BadRequest asking for the user to extend the other record instead.
                        {
                            return BadRequest(new { message = "Extend your other existing account rate! Other existing account rate with the same rate/hour of $" + ar.RatePerHour + " is successive with your new account rate." });

                        }
                    }

                    //If the record has no overlap and non-continuous, save the edited AccountRate record to database and return a Ok 200 response.
                    Database.AccountRates.Update(arToUpdate);
                    Database.SaveChanges();
                    return Ok(new { message = "Successfully updated record." });

                }
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "There are issues with the request." });
            }
        }

        // DELETE api/<controller>/5
        [Authorize("ADMIN")]
        [HttpDelete("Delete/{cusId}/{rateId}")]
        public IActionResult Delete(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(r => r.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId); //Retrieve the specific AccountRate record with the specified AccountRateId
                if (ar == null) //If AccountRate record is not found return NotFound response
                {
                    return NotFound(new { message = "Customer account rate could not be found" });
                }

                if(Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).Count() > 0)
                {
                    List<AccountTimeTable> tt = Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).ToList();
                    Database.AccountTimeTable.RemoveRange(tt); //Remove all AccountTimeTables record related to the account rate
                }
                //Remove all record from the AccountRate tables related to the AccountRateId
                Database.AccountRates.Remove(ar);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Deleted Account Rate record with acommpanying TimeTable records" }); //Return Ok 200 to tell the user the records has been successfully deleted.
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });  //If any database related operation occur, return ISE
            }
        }
    }

    public class QueryPagingParametersForAccountRates
    {
        //Query parameters for pagination
        [BindRequired]
        public int page_number { get; set; }
        public int per_page { get; set; }
        public string sort_order { get; set; }

    }//end of QueryPagingParametersForAccountRates class
}
