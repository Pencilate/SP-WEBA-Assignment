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

                if((currentPage == totalPage) && (totalRecords % pageSize != 0))
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

        [Authorize("ADMIN")]
        [HttpGet("{cusId}/{rateId}")]
        public IActionResult GetRate(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId);
                if(ar == null)
                {
                    return NotFound(new { message = "Customer account rate could not be found" });

                }
                object arObject = new
                {
                    rate = ar.RatePerHour,
                    startDate = ar.EffectiveStartDate,
                    endDate = ar.EffectiveEndDate
                };
                return Ok(arObject);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
        }

        [Authorize("ADMIN")]
        [HttpGet("RateSummary/{cusId}/{rateId}")]
        public IActionResult GetAccountRateSummary(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId);
                if (ar == null)
                {
                    return NotFound(new { message = "Customer account rate could not be found" });
                }
                int ttCount = Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).Count();

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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
        }

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("Create/{id}")]
        public IActionResult Post(int id, [FromForm]IFormCollection data)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid").Value);

                AccountRate newAR = new AccountRate();
                newAR.CustomerAccountId = id;
                newAR.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                newAR.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                newAR.RatePerHour = decimal.Parse(data["accountRate"]);
                newAR.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                newAR.CreatedById = userId;
                newAR.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                newAR.UpdatedById = userId;

                List<AccountRate> allAR = Database.AccountRates.Where(r => r.CustomerAccountId == id).ToList();
                bool overlap = false;
                List<String> overlapMsg = new List<string>();
                foreach (AccountRate ar in allAR)
                {
                    bool overlapCurrentRecord = false;
                    if (newAR.EffectiveStartDate.CompareTo(ar.EffectiveStartDate) >= 0)//After
                    {
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) <= 0)//Before
                        {
                            overlapCurrentRecord = true; //Record overlap the entire period
                        }
                        if (newAR.EffectiveStartDate.CompareTo(ar.EffectiveEndDate) <= 0)//Before
                        {
                            overlapCurrentRecord = true; // Record overlap front of period
                        }

                    }
                    else//Before
                    {
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) >= 0)//After
                        {
                            overlapCurrentRecord = true; //Record is encapulated by period
                        }
                        if (newAR.EffectiveEndDate.CompareTo(ar.EffectiveStartDate) >= 0)//After
                        {
                            overlapCurrentRecord = true; //Record over back of period
                        }
                    }
                    if (overlapCurrentRecord)
                    {
                        overlap = true;
                        overlapMsg.Add(ar.EffectiveStartDate.ToString("d") + " and " + ar.EffectiveEndDate.ToString("d"));
                    }
                }


                if (overlap)
                {
                    String message = "Overlaps with existing rate dates! Dates cannot be between ";
                    for (int i = 0; i < overlapMsg.Count; i++)
                    {
                        if (i > 0)
                        {
                            message += " or ";
                        }
                        message += overlapMsg[i] + " inclusive";
                    }
                    return BadRequest(new { message = message });
                }
                else
                {
                    foreach (AccountRate ar in allAR)
                    {
                        if ((newAR.RatePerHour == ar.RatePerHour) && ((newAR.EffectiveStartDate.AddDays(-1).Date.CompareTo(ar.EffectiveEndDate) == 0) || (newAR.EffectiveEndDate.AddDays(1).Date.CompareTo(ar.EffectiveStartDate) == 0)))
                        {
                            return BadRequest(new { message = "Extend your existing account rate! Existing account rate with the same rate/hour of $" + ar.RatePerHour + " is successive with your new account rate." });

                        }
                    }

                    Database.AccountRates.Add(newAR);
                    Database.SaveChanges();
                    return Ok(new { message = "Successfully added record." });

                }
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

        // PUT api/<controller>/5
        [Authorize("ADMIN")]
        [HttpPut("Update/{cusId}/{rateId}")]
        public IActionResult UpdateCustomerAccountRate(int cusId, int rateId, [FromForm]IFormCollection data)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("userid").Value);

                AccountRate arToUpdate = Database.AccountRates.Where(c => c.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId);
                arToUpdate.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                arToUpdate.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                arToUpdate.RatePerHour = decimal.Parse(data["accountRate"]);
                arToUpdate.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                arToUpdate.UpdatedById = userId;

                List<AccountRate> allAR = Database.AccountRates.Where(r => r.CustomerAccountId == cusId).Where(r => r.AccountRateId != rateId).ToList();
                bool overlap = false;
                List<String> overlapMsg = new List<string>();
                foreach (AccountRate ar in allAR)
                {
                    bool overlapCurrentRecord = false;
                    if (arToUpdate.EffectiveStartDate.CompareTo(ar.EffectiveStartDate) >= 0)//After
                    {
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) <= 0)//Before
                        {
                            overlapCurrentRecord = true; //Record overlap the entire period
                        }
                        if (arToUpdate.EffectiveStartDate.CompareTo(ar.EffectiveEndDate) <= 0)//Before
                        {
                            overlapCurrentRecord = true; // Record overlap front of period
                        }

                    }
                    else//Before
                    {
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveEndDate) >= 0)//After
                        {
                            overlapCurrentRecord = true; //Record is encapulated by period
                        }
                        if (arToUpdate.EffectiveEndDate.CompareTo(ar.EffectiveStartDate) >= 0)//After
                        {
                            overlapCurrentRecord = true; //Record over back of period
                        }
                    }
                    if (overlapCurrentRecord)
                    {
                        overlap = true;
                        overlapMsg.Add(ar.EffectiveStartDate.ToString("d") + " and " + ar.EffectiveEndDate.ToString("d"));
                    }
                }


                if (overlap)
                {
                    String message = "Overlaps with other existing rate dates! Dates cannot be between ";
                    for (int i = 0; i < overlapMsg.Count; i++)
                    {
                        if (i > 0)
                        {
                            message += " or ";
                        }
                        message += overlapMsg[i] + " inclusive";
                    }
                    return BadRequest(new { message = message });
                }
                else
                {
                    foreach (AccountRate ar in allAR)
                    {
                        if ((arToUpdate.RatePerHour == ar.RatePerHour) && ((arToUpdate.EffectiveStartDate.AddDays(-1).Date.CompareTo(ar.EffectiveEndDate) == 0) || (arToUpdate.EffectiveEndDate.AddDays(1).Date.CompareTo(ar.EffectiveStartDate) == 0)))
                        {
                            return BadRequest(new { message = "Extend your other existing account rate! Other existing account rate with the same rate/hour of $" + ar.RatePerHour + " is successive with your new account rate." });

                        }
                    }
                    Database.AccountRates.Update(arToUpdate);
                    Database.SaveChanges();
                    return Ok(new { message = "Successfully updated record." });

                }
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

        // DELETE api/<controller>/5
        [Authorize("ADMIN")]
        [HttpDelete("Delete/{cusId}/{rateId}")]
        public IActionResult Delete(int cusId, int rateId)
        {
            try
            {
                AccountRate ar = Database.AccountRates.Where(r => r.CustomerAccountId == cusId).SingleOrDefault(r => r.AccountRateId == rateId);
                if(ar == null)
                {
                    return NotFound(new { message = "Customer account rate could not be found" });
                }
                if(Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).Count() > 0) //This was done as the name of EffectiveStartDate/EffectiveEndDate in the AccountTimeTable model and The AccountTimeTable Table in the database do not match
                {
                    //List<AccountTimeTable> tt = Database.AccountTimeTable.Where(t => t.AccountRateId == rateId).ToList();
                    //Database.AccountTimeTable.RemoveRange(tt);
                }
                
                Database.AccountRates.Remove(ar);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Deleted Account Rate record with acommpanying TimeTable records" });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
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
