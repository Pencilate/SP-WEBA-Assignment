﻿using System;
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
    public class InstructorsController : Controller
    {
        private IAppDateTimeService _appDateTimeService;
        public ApplicationDbContext Database { get; }
        //The following constructor code pattern is required for every Web API
        //controller class.
        public InstructorsController(IAppDateTimeService appDateTimeService, ApplicationDbContext database)
        {
            Database = database;
            _appDateTimeService = appDateTimeService;
        }

        // GET api/<controller>/5
        [Authorize("ADMIN")]
        [HttpGet("GetIAPaginated/{id}")]
        public IActionResult Get(int id, [FromQuery]QueryPagingParametersForInstructorAccount inParameters)
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
                string searchTerm = "";

                int totalPage = 0;
                int startRecord = 0;
                int endRecord = 0;
                int totalRecords = 0;
                if (ModelState.IsValid) //if query params is valid load the data into pagination parameters
                {
                    currentPage = Int32.Parse(inParameters.page_number.ToString());
                    pageSize = Int32.Parse(inParameters.per_page.ToString());
                    sortOrder = inParameters.sort_order;
                    searchTerm = inParameters.search_term;
                }
                else
                {
                    currentPage = 1;
                    pageSize = 10;
                    sortOrder = "ASC";
                    searchTerm = "";
                }
                if (currentPage == 1)  //Calculate the page's first record's index
                {
                    startRecord = 1;
                }
                else
                {
                    startRecord = ((currentPage - 1) * pageSize) + 1;
                }

                List <InstructorAccount> cusInstrctorAccounts; //Declare a list to store all the InstructorAccount records related to the customer from the database
                if (sortOrder.Equals("ASC"))
                {
                    //Query the database for InstructorAccount records related to the customer in the ascending order of InstructorAccountId
                    if (String.IsNullOrEmpty(searchTerm))
                    {
                        cusInstrctorAccounts = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Include(ia => ia.Instructor).Include(ia => ia.CreatedBy).OrderBy(ia => ia.InstructorAccountId).ToList();
                    }
                    else
                    {
                        cusInstrctorAccounts = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Include(ia => ia.Instructor).Include(ia => ia.CreatedBy).Where(ia => ia.Instructor.FullName.Contains(searchTerm)).OrderBy(ia => ia.InstructorAccountId).ToList();
                    }
                }
                else if (sortOrder.Equals("DESC"))
                {
                    //Query the database for InstructorAccount records related to the customer in the descending order of InstructorAccountId
                    if (String.IsNullOrEmpty(searchTerm))
                    {
                        cusInstrctorAccounts = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Include(ia => ia.Instructor).Include(ia => ia.CreatedBy).OrderByDescending(ia => ia.InstructorAccountId).ToList();
                    }
                    else
                    {
                        cusInstrctorAccounts = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Include(ia => ia.Instructor).Include(ia => ia.CreatedBy).Where(ia => ia.Instructor.FullName.Contains(searchTerm)).OrderByDescending(ia => ia.InstructorAccountId).ToList();
                    }
                    
                }
                else
                {
                    //return BadRequest for invalid sort order
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = cusInstrctorAccounts.Count; //Get the total no of InstructorAccount records from the datebase

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate the total number of pages
                if (totalRecords == 0)
                {
                    //If no records are found for the InstructorAccount
                    return Ok(new
                    {
                        accountName = ca.AccountName,
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

                //Iterate through the list of InstructorAccount, and cherry pick the data to return
                List<object> cusInstructorAccountRateByPage = new List<object>();
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

                    //Find the list of Customer Assigned to the Instructor
                    string assignedCusAccounts = "";
                    List<InstructorAccount> iaCusAssigned = Database.InstructorAccounts.Where(ia => ia.InstructorId == cusInstrctorAccounts[i].InstructorId).Include(ia => ia.CustomerAccount).ToList();
                    foreach (InstructorAccount iar in iaCusAssigned)
                    {
                        assignedCusAccounts += iar.CustomerAccount.AccountName + ",";
                    }
                    if (!string.IsNullOrEmpty(assignedCusAccounts))
                    {
                        assignedCusAccounts = assignedCusAccounts.Remove(assignedCusAccounts.Length - 1);
                    }

                    cusInstructorAccountRateByPage.Add(new
                    {
                        no = recordNo,
                        id = cusInstrctorAccounts[i].InstructorAccountId,
                        name = cusInstrctorAccounts[i].Instructor.FullName,
                        email = cusInstrctorAccounts[i].Instructor.UserName,
                        wage = cusInstrctorAccounts[i].WageRate,
                        accountsAssigned = assignedCusAccounts,
                        createdAt = cusInstrctorAccounts[i].CreatedAt,
                        assignedBy = cusInstrctorAccounts[i].CreatedBy.FullName
                    });
                }

                //Craft the response object to return   
                object result = new
                {
                    accountName = ca.AccountName,
                    totalRecordCount = totalRecords,
                    totalCurrentPgRec = cusInstructorAccountRateByPage.Count,
                    currentPage = currentPage,
                    totalPage = totalPage,
                    records = cusInstructorAccountRateByPage,
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
        [HttpGet("GetAllInstructorPaginated/{id}")]
        public IActionResult GetAllInstructorPaginated(int id, [FromQuery]QueryPagingParametersForInstructorAccount inParameters)
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
                string searchTerm = "";

                int totalPage = 0;
                int startRecord = 0;
                int endRecord = 0;
                int totalRecords = 0;
                if (ModelState.IsValid) //if query params is valid load the data into pagination parameters
                {
                    currentPage = Int32.Parse(inParameters.page_number.ToString());
                    pageSize = Int32.Parse(inParameters.per_page.ToString());
                    sortOrder = inParameters.sort_order;
                    searchTerm = inParameters.search_term;
                }
                else
                {
                    currentPage = 1;
                    pageSize = 10;
                    sortOrder = "ASC";
                    searchTerm = "";
                }
                if (currentPage == 1)  //Calculate the page's first record's index
                {
                    startRecord = 1;
                }
                else
                {
                    startRecord = ((currentPage - 1) * pageSize) + 1;
                }

                List<AppUser> instrctors; //Declare a list to store all the Instructor records related to the customer from the database
                if (sortOrder.Equals("ASC"))
                {
                    //Query the database for Instructor AppUser records related to the customer in the ascending order of Id
                    if (String.IsNullOrEmpty(searchTerm))
                    {
                        instrctors = Database.AppUsers.Where(au => au.RoleId == 2).OrderBy(au => au.Id).ToList();
                    }
                    else{
                        instrctors = Database.AppUsers.Where(au => au.RoleId == 2).Where(au => au.FullName.Contains(searchTerm)).OrderBy(au => au.Id).ToList();
                    }
                }
                else if (sortOrder.Equals("DESC"))
                {
                    //Query the database for Instructor AppUser records related to the customer in the descending order of Id
                    if (String.IsNullOrEmpty(searchTerm))
                    {
                        instrctors = Database.AppUsers.Where(au => au.RoleId == 2).OrderByDescending(au => au.Id).ToList();
                    }
                    else
                    {
                        instrctors = Database.AppUsers.Where(au => au.RoleId == 2).Where(au => au.FullName.Contains(searchTerm)).OrderByDescending(au => au.Id).ToList();
                    }
                }
                else
                {
                    //return BadRequest for invalid sort order
                    return BadRequest(new { message = "Invalid Sort Order" });
                }

                totalRecords = instrctors.Count; //Get the total no of AppUser records from the datebase

                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate the total number of pages
                if (totalRecords == 0)
                {
                    //If no records are found for the customer
                    return Ok(new
                    {
                        accountName = ca.AccountName,
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

                //Iterate through the list of Instructor AppUsers, and cherry pick the data to return
                List<object> cusInstructorAccountRateByPage = new List<object>();
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

                    //Find the list of Customer Assigned to the Instructor
                    string assignedCusAccounts = "";
                    List<InstructorAccount> iaCusAssigned = Database.InstructorAccounts.Where(ia => ia.InstructorId == instrctors[i].Id).Include(ia => ia.CustomerAccount).ToList();
                    foreach (InstructorAccount iar in iaCusAssigned) {
                        assignedCusAccounts += iar.CustomerAccount.AccountName + ",";
                    }
                    if (!string.IsNullOrEmpty(assignedCusAccounts))
                    {
                        assignedCusAccounts = assignedCusAccounts.Remove(assignedCusAccounts.Length - 1);
                    }

                    //Check if the instructor is already assigned to Customer
                    bool assigned = false;
                    decimal? wage = null;
                    InstructorAccount iaRec = Database.InstructorAccounts.Where(c => c.CustomerAccountId == id).SingleOrDefault(ia => ia.InstructorId == instrctors[i].Id);
                    if (iaRec != null) {
                        assigned = true;
                        wage = iaRec.WageRate; 
                    }

                    cusInstructorAccountRateByPage.Add(new
                    {
                        no = recordNo,
                        id = instrctors[i].Id,
                        name = instrctors[i].FullName,
                        email = instrctors[i].UserName,
                        isAlreadyAssignedToCustomer = assigned,
                        wageRate = wage,
                        accountsAssigned = assignedCusAccounts 
                    });
                }

                //Craft the response object to return   
                object result = new
                {
                    accountName = ca.AccountName,
                    totalRecordCount = totalRecords,
                    totalCurrentPgRec = cusInstructorAccountRateByPage.Count,
                    currentPage = currentPage,
                    totalPage = totalPage,
                    records = cusInstructorAccountRateByPage,
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

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("Assign")]
        public IActionResult Post([FromForm]IFormCollection  data)
        {
            //API Data Validation
            int testInt = 0;
            bool testBool = false;
            decimal testDecimal = 0;
            bool validDates = true;
            bool validIDs = true;
            List<String> errorMessages = new List<string>();
            if (!data.ContainsKey("customerId"))
            {
                errorMessages.Add("Customer Account ID is missing. ");
            }
            if (!data.ContainsKey("instructorId"))
            {
                errorMessages.Add("Instructor ID is missing. ");
            }
            if (!data.ContainsKey("wageRate"))
            {
                errorMessages.Add("Wage Rate is missing. ");
            }
            if (!int.TryParse(data["customerId"].ToString(), out testInt))
            {
                errorMessages.Add("Customer Account ID MUST be numeric. ");
                validIDs = false;
            }
            if (Database.CustomerAccounts.SingleOrDefault(ca => ca.CustomerAccountId == testInt) == null)
            {
                errorMessages.Add("Customer does not exist. ");
            }
            if (!int.TryParse(data["instructorId"].ToString(), out testInt))
            {
                errorMessages.Add("Instructor ID MUST be numeric. ");
                validIDs = false;   
            }
            if (Database.AppUsers.Where(au => au.RoleId == 2).SingleOrDefault(au => au.Id == testInt) == null)
            {
                errorMessages.Add("Instructor does not exist. ");
            }
            if (!decimal.TryParse(data["wageRate"].ToString(), out testDecimal))
            {
                errorMessages.Add("Wage Rate MUST be a decimal. ");
            }
            if (validIDs) {
                if (Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == int.Parse(data["customerId"].ToString())).SingleOrDefault(ia => ia.InstructorAccountId == int.Parse(data["customerId"].ToString())) != null) {
                    errorMessages.Add("Instructor Already Assigned to Customer. ");
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
                //Populate InstructorAccount with data from sent by client
                InstructorAccount newIA = new InstructorAccount();
                newIA.CustomerAccountId = int.Parse(data["customerId"]);
                newIA.InstructorId = int.Parse(data["instructorId"]);
                newIA.WageRate = decimal.Parse(data["wageRate"]);
                newIA.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                newIA.CreatedById = userId;

                //Save changes to database
                Database.InstructorAccounts.Add(newIA);
                Database.SaveChanges();

               return Ok(new { message = "Successfully assigned instructor"});
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
        [HttpDelete("Unassign")]
        public IActionResult Delete([FromQuery] string value) 
        {

            List<string> IAid = value.Split(',').ToList();

            //API Data Validation
            int testInt = 0;

            List<String> errorMessages = new List<string>();
            foreach (string val in IAid) {
                if (!int.TryParse(val, out testInt))
                {
                    errorMessages.Add(val + " is not a valid Instructor Account ID");
                }
                else {
                    if (Database.InstructorAccounts.SingleOrDefault(ia => ia.InstructorAccountId == testInt) == null)
                    {
                        errorMessages.Add("InstructorAccount "+val+" does not exist");
                    }
                }
            }

            string errorString = "";
            errorString = string.Concat(errorMessages);

            if (errorMessages.Count > 0)
            {
                return BadRequest(new { message = errorString });
            }
            //Remove Records and Save Changes to Database
            List<InstructorAccount> iaList = new List<InstructorAccount>();
            try {
               iaList  = Database.InstructorAccounts.Where(record => IAid.Contains(record.InstructorAccountId.ToString())).ToList();
                if (iaList != null) {
                    Database.InstructorAccounts.RemoveRange(iaList);
                }
                Database.SaveChanges();
                return Ok(new { message = "Successfully unassgined instructors" });
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
    }
    public class QueryPagingParametersForInstructorAccount
    {
        //Query parameters for pagination
        [BindRequired]
        public int page_number { get; set; }
        public int per_page { get; set; }
        public string sort_order { get; set; }
        public string search_term { get; set; }
        

    }//end of QueryPagingParametersForAccountRates class
}
