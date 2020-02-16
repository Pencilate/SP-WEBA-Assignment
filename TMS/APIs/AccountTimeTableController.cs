using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
                if (ar == null) //If AccountRate does not exist in database, return 404 Not Found
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
                    //Query the database for AccountTimeTable records related to the customer in the ascending order of AccountTimeTableId
                    cusAccountTimeTable = Database.AccountTimeTable.Where(catt => catt.AccountRateId == arID).OrderBy(catt => catt.AccountTimeTableId).ToList();
                }
                else if (sortOrder.Equals("DESC"))
                {
                    //Query the database for AccountTimeTable records related to the customer in the descending order of AccountTimeTableId
                    cusAccountTimeTable = Database.AccountTimeTable.Where(catt => catt.AccountRateId == arID).OrderByDescending(catt => catt.AccountTimeTableId).ToList();
                }
                else
                {
                    //return BadRequest for invalid sort order
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = cusAccountTimeTable.Count; //Get the total no of AccountTimeTable records from the datebase

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate the total number of pages
                if (totalRecords == 0)
                {
                    //If no records are found for the accountTimeTable
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

                //Iterate through the list of AccountTimeTable, and cherry pick the data to return
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
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
        }

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("Create")]
        public IActionResult Post([FromForm]IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

            //API Data Validation
            int testInt = 0;
            bool testBool = false;
            DateTime testStartDate = DateTime.Now;
            DateTime testEndDate = DateTime.Now;
            bool validDates = true;
            bool validDayWeek = true;

            List<String> errorMessages = new List<string>();
            if (!data.ContainsKey("id"))
            {
                errorMessages.Add("Account Rate ID is missing. ");
            }
            if (!data.ContainsKey("dayOfWeek")) {
                errorMessages.Add("Day of Week is missing. ");
            }
            if (!data.ContainsKey("startDateTime"))
            {
                errorMessages.Add("Start Date and Time is missing. ");
            }
            if (!data.ContainsKey("endDateTime"))
            {
                errorMessages.Add("End Date and Time is missing. ");
            }
            if (!data.ContainsKey("visibility"))
            {
                errorMessages.Add("Account visiibility boolean is missing. ");
            }
            if (!data.ContainsKey("override"))
            {
                errorMessages.Add("Override boolean is missing. ");
            }
            if (!int.TryParse(data["id"].ToString(), out testInt)) {
                errorMessages.Add("Account Rate ID MUST be numeric. ");
            }
            if (!int.TryParse(data["dayOfWeek"].ToString(), out testInt))
            {
                errorMessages.Add("Day Of Week MUST be numeric. ");
                validDayWeek = false;
            }
            if (validDayWeek) {
                if (testInt < 1 || testInt > 7) {
                    errorMessages.Add("Day Of Week can only be from 1 to 7. ");
                }
            }
            if (!bool.TryParse(data["visibility"].ToString(), out testBool))
            {
                errorMessages.Add("Visibility can only be true or false. ");
            }
            if (!bool.TryParse(data["override"].ToString(), out testBool))
            {
                errorMessages.Add("Override can only be true or false. ");
            }
            if (!DateTime.TryParseExact(data["startDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out testStartDate))
            {
                validDates = false;
                errorMessages.Add("Data contains invalid Start Date and Time. ");
            }
            if (!DateTime.TryParseExact(data["endDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out testEndDate))
            {
                validDates = false;
                errorMessages.Add("Data contains invalid End Date and Time. ");
            }
            if (validDates) {
                if (testStartDate.Date.CompareTo(testEndDate.Date) > 0) {
                    errorMessages.Add("Start Date must be earlier then End Date. ");
                }
                if (testStartDate.TimeOfDay.CompareTo(testEndDate.TimeOfDay) > 0) {
                    errorMessages.Add("Start Time must be earlier then End Time. ");

                }
            }

            string errorString = "";
            errorString = string.Concat(errorMessages);

            if (errorMessages.Count > 0)
            {
                return BadRequest(new { message = errorString });
            }

            try
            {
                //Populate AccountTimeTable with data sent from client
                AccountTimeTable newATT = new AccountTimeTable();
                int ARID = int.Parse(data["id"]);
                newATT.AccountRateId = ARID;
                newATT.DayOfWeekNumber = int.Parse(data["dayOfWeek"]);
                newATT.EffectiveStartDateTime = DateTime.ParseExact(data["startDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture);
                newATT.EffectiveEndDateTime = DateTime.ParseExact(data["endDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture);
                newATT.IsVisible = Boolean.Parse(data["visibility"]);
                newATT.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                newATT.CreatedById = userId;
                newATT.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                newATT.UpdatedById = userId;

                //Variables for checking if the account timetable data sent by the client clashes with any existing records in the database
                bool forceOverride = Boolean.Parse(data["override"]);
                bool overlap = false;
                bool identical = false;
                List<AccountTimeTable> identicalATT = new List<AccountTimeTable>();
                List<AccountTimeTable> overlapATT = new List<AccountTimeTable>();

                //Setting up the Stored Procedure call for checking Identical and Overlap records
                SqlParameter[] parameters = new SqlParameter[4];
                parameters[0] = new SqlParameter("@AccountRateId", SqlDbType.Int);
                parameters[0].Value = newATT.AccountRateId;
                parameters[1] = new SqlParameter("@DayOfWeek", SqlDbType.Int);
                parameters[1].Value = newATT.DayOfWeekNumber;
                parameters[2] = new SqlParameter("@StartDateTime", SqlDbType.DateTime2);
                parameters[2].Value = newATT.EffectiveStartDateTime;
                parameters[3] = new SqlParameter("@EndDateTime", SqlDbType.DateTime2);
                parameters[3].Value = newATT.EffectiveEndDateTime;

                //Executing Stored Procedure For Identical records
                identicalATT = Database.AccountTimeTable.FromSql($"exec uspAccountTimeTableCheckIdenticalBeforeAdd @AccountRateId, @DayOfWeek, @StartDateTime, @EndDateTime",parameters).ToList();
                if (identicalATT != null && identicalATT.Count > 0) {
                    identical = true;
                }
                //Executing Stored Procedure For Overlap records
                overlapATT = Database.AccountTimeTable.FromSql($"exec uspAccountTimeTableCheckOverlapBeforeAdd @AccountRateId, @DayOfWeek, @StartDateTime, @EndDateTime", parameters).ToList();
                if (overlapATT != null && overlapATT.Count > 0)
                {
                    overlap = true;
                }

                
                if (identical)
                {//If there are identical records, send the records back to the client as JSON along with error message and override indication
                    List<object> result = new List<object>();
                    foreach (AccountTimeTable iATT in identicalATT) {
                        result.Add(new
                        {
                            id = iATT.AccountTimeTableId,
                            day = iATT.DayOfWeekNumber,
                            startTime = iATT.EffectiveStartDateTime.TimeOfDay,
                            endTime = iATT.EffectiveEndDateTime.TimeOfDay,
                            startDate = iATT.EffectiveStartDateTime.Date,
                            endDate = iATT.EffectiveEndDateTime.Date,
                            visibility = iATT.IsVisible
                        });
                        return BadRequest(new { overridable = false, message = "Failed to create record. Your account timetable is the same as the following existing record.", record = result });
                    }
                } else if (overlap && !forceOverride)
                { //If there areoverlap records and the client did not request override, send the records back to the client as JSON along with error message and override indication

                    List<object> result = new List<object>();
                    foreach (AccountTimeTable oATT in overlapATT)
                    {
                        result.Add(new
                        {
                            id = oATT.AccountTimeTableId,
                            day = oATT.DayOfWeekNumber,
                            startTime = oATT.EffectiveStartDateTime.TimeOfDay,
                            endTime = oATT.EffectiveEndDateTime.TimeOfDay,
                            startDate = oATT.EffectiveStartDateTime.Date,
                            endDate = oATT.EffectiveEndDateTime.Date,
                            visibility = oATT.IsVisible
                        });
                    }

                    return BadRequest(new { overridable = true, message = "Your timetable overlaps with the following existing timetable. Continue creating?", record = result });
                }

                //If there is no identical records or overlap records with force insert, save the record to database
                Database.AccountTimeTable.Add(newATT);
                Database.SaveChanges();
                return Ok(new { message = "Successfully added record." });

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

        // PUT api/<controller>/5
        [Authorize("ADMIN")]
        [HttpPut("Update/{ttid}")]
        public IActionResult Put(int ttid, [FromForm]IFormCollection data)
        {
            //API Data Validation
            int testInt = 0;
            bool testBool = false;
            DateTime testStartDate = DateTime.Now;
            DateTime testEndDate = DateTime.Now;
            bool validDates = true;
            bool validDayWeek = true;

            List<String> errorMessages = new List<string>();
            if (!data.ContainsKey("id"))
            {
                errorMessages.Add("Account Rate ID is missing. ");
            }
            if (!data.ContainsKey("dayOfWeek"))
            {
                errorMessages.Add("Day of Week is missing. ");
            }
            if (!data.ContainsKey("startDateTime"))
            {
                errorMessages.Add("Start Date and Time is missing. ");
            }
            if (!data.ContainsKey("endDateTime"))
            {
                errorMessages.Add("End Date and Time is missing. ");
            }
            if (!data.ContainsKey("visibility"))
            {
                errorMessages.Add("Account visiibility boolean is missing. ");
            }
            if (!data.ContainsKey("override"))
            {
                errorMessages.Add("Override boolean is missing. ");
            }
            if (!int.TryParse(data["id"].ToString(), out testInt))
            {
                errorMessages.Add("Account Rate ID MUST be numeric. ");
            }
            if (!int.TryParse(data["dayOfWeek"].ToString(), out testInt))
            {
                errorMessages.Add("Day Of Week MUST be numeric. ");
                validDayWeek = false;
            }
            if (validDayWeek)
            {
                if (testInt < 1 || testInt > 7)
                {
                    errorMessages.Add("Day Of Week can only be from 1 to 7. ");
                }
            }
            if (!bool.TryParse(data["visibility"].ToString(), out testBool))
            {
                errorMessages.Add("Visibility can only be true or false. ");
            }
            if (!bool.TryParse(data["override"].ToString(), out testBool))
            {
                errorMessages.Add("Override can only be true or false. ");
            }
            if (!DateTime.TryParseExact(data["startDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out testStartDate))
            {
                validDates = false;
                errorMessages.Add("Data contains invalid Start Date and Time. ");
            }
            if (!DateTime.TryParseExact(data["endDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out testEndDate))
            {
                validDates = false;
                errorMessages.Add("Data contains invalid End Date and Time. ");
            }
            if (validDates)
            {
                if (testStartDate.Date.CompareTo(testEndDate.Date) > 0)
                {
                    errorMessages.Add("Start Date must be earlier then End Date. ");
                }
                if (testStartDate.TimeOfDay.CompareTo(testEndDate.TimeOfDay) > 0)
                {
                    errorMessages.Add("Start Time must be earlier then End Time. ");

                }
            }

            string errorString = "";
            errorString = string.Concat(errorMessages);

            if (errorMessages.Count > 0)
            {
                return BadRequest(new { message = errorString });
            }

            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user
            try
            {
                AccountTimeTable foundATT = Database.AccountTimeTable.SingleOrDefault(tt => tt.AccountTimeTableId == ttid);
                if (foundATT == null) //If AccountTimeTable record is not found return NotFound response
                {
                    return NotFound(new { message = "Customer account timetable could not be found" });
                }
                //Update the information in the existing Account Timetable record
                int ARID = int.Parse(data["id"]);
                foundATT.DayOfWeekNumber = int.Parse(data["dayOfWeek"]);
                foundATT.EffectiveStartDateTime = DateTime.ParseExact(data["startDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture);
                foundATT.EffectiveEndDateTime = DateTime.ParseExact(data["endDateTime"], "d/M/yyyy h : mm tt", System.Globalization.CultureInfo.InvariantCulture);
                foundATT.IsVisible = Boolean.Parse(data["visibility"]);
                foundATT.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                foundATT.UpdatedById = userId;

                //Variables for checking if the account timetable data sent by the client clashes with any existing records in the database
                bool forceOverride = Boolean.Parse(data["override"]);
                List<AccountTimeTable> existingATT = Database.AccountTimeTable.Where(a => a.AccountRateId == ARID).Where(tt => tt.AccountTimeTableId != ttid).ToList();
                bool overlap = false;
                bool identical = false;
                List<AccountTimeTable> identicalATT = new List<AccountTimeTable>();
                List<AccountTimeTable> overlapATT = new List<AccountTimeTable>();

                //Setting up the Stored Procedure call for checking Identical and Overlap records
                SqlParameter[] parameters = new SqlParameter[5];
                parameters[0] = new SqlParameter("@AccountTimeTableId", SqlDbType.Int);
                parameters[0].Value = foundATT.AccountTimeTableId;
                parameters[1] = new SqlParameter("@AccountRateId", SqlDbType.Int);
                parameters[1].Value = foundATT.AccountRateId;
                parameters[2] = new SqlParameter("@DayOfWeek", SqlDbType.Int);
                parameters[2].Value = foundATT.DayOfWeekNumber;
                parameters[3] = new SqlParameter("@StartDateTime", SqlDbType.DateTime2);
                parameters[3].Value = foundATT.EffectiveStartDateTime;
                parameters[4] = new SqlParameter("@EndDateTime", SqlDbType.DateTime2);
                parameters[4].Value = foundATT.EffectiveEndDateTime;

                //Executing Stored Procedure For Identical records
                identicalATT = Database.AccountTimeTable.FromSql($"exec uspAccountTimeTableCheckIdenticalBeforeUpdate @AccountTimeTableId,  @AccountRateId, @DayOfWeek, @StartDateTime, @EndDateTime", parameters).ToList();
                if (identicalATT != null && identicalATT.Count > 0)
                {
                    identical = true;
                }
                //Executing Stored Procedure For Overlap records
                overlapATT = Database.AccountTimeTable.FromSql($"exec uspAccountTimeTableCheckOverlapBeforeUpdate @AccountTimeTableId,  @AccountRateId, @DayOfWeek, @StartDateTime, @EndDateTime", parameters).ToList();
                if (overlapATT != null && overlapATT.Count > 0)
                {
                    overlap = true;
                }

                if (identical)
                {//If there are identical records, send the records back to the client as JSON along with error message and override indication
                    List<object> result = new List<object>();
                    foreach (AccountTimeTable iATT in identicalATT)
                    {
                        result.Add(new
                        {
                            id = iATT.AccountTimeTableId,
                            day = iATT.DayOfWeekNumber,
                            startTime = iATT.EffectiveStartDateTime.TimeOfDay,
                            endTime = iATT.EffectiveEndDateTime.TimeOfDay,
                            startDate = iATT.EffectiveStartDateTime.Date,
                            endDate = iATT.EffectiveEndDateTime.Date,
                            visibility = iATT.IsVisible
                        });
                        return BadRequest(new { overridable = false, message = "Failed to update record. Your account timetable is the same as the following existing record.", record = result });
                    }
                }
                else if (overlap && !forceOverride)
                {//If there areoverlap records and the client did not request override, send the records back to the client as JSON along with error message and override indication

                    List<object> result = new List<object>();
                    foreach (AccountTimeTable oATT in overlapATT)
                    {
                        result.Add(new
                        {
                            id = oATT.AccountTimeTableId,
                            day = oATT.DayOfWeekNumber,
                            startTime = oATT.EffectiveStartDateTime.TimeOfDay,
                            endTime = oATT.EffectiveEndDateTime.TimeOfDay,
                            startDate = oATT.EffectiveStartDateTime.Date,
                            endDate = oATT.EffectiveEndDateTime.Date,
                            visibility = oATT.IsVisible
                        });
                    }

                    return BadRequest(new { overridable = true, message = "Your timetable overlaps with the following existing timetable. Continue updating?", record = result });
                }

                //If there is no identical records or overlap records with force insert, save the record to database
                Database.AccountTimeTable.Update(foundATT);
                Database.SaveChanges();
                return Ok(new { message = "Successfully updated record." });

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
        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                AccountTimeTable att = Database.AccountTimeTable.SingleOrDefault(tt => tt.AccountTimeTableId == id);
                if (att == null) //If AccountTimeTable record is not found return NotFound response
                {
                    return NotFound(new { message = "Customer account timetable could not be found" });
                }

                //Remove all record from the AccountTimeTable tables
                Database.AccountTimeTable.Remove(att);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Deleted Account TimeTable record" }); //Return Ok 200 to tell the user the records has been successfully deleted.
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });  //If any database related operation occur, return ISE
            }
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
