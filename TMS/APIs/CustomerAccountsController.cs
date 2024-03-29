﻿using System;
using System.Collections.Generic;
using System.Data.Common;
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
                List<CustomerAccount> cusAccQueryList = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).ToList();//Query DB for all the customer accounts


                foreach (CustomerAccount ca in cusAccQueryList)//Loop through each customer account in the queried customer account list
                {
                    //Cherry pick specific information to return as a response
                    cusAccList.Add(new
                    {
                        id = ca.CustomerAccountId,
                        accountName = ca.AccountName,
                        //Query the Customer Account Comments table to retrieve the number of comments recorded in the last 3 days
                        comments = Database.CustomerAccountComments.Where(c => c.UpdatedAt.CompareTo(_appDateTimeService.GetCurrentDateTime().Date.AddDays(-3)) > 0).Count(c => c.CustomerAccountId == ca.CustomerAccountId),
                        visibility = ca.IsVisible,
                        createdBy = ca.CreatedBy.FullName,
                        updatedBy = ca.UpdatedBy.FullName,
                        updatedAt = ca.UpdatedAt
                    });
                }
                return Ok(cusAccList);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
           
        }

        // GET: api/<controller>
        [Authorize("ADMIN")]
        [HttpGet("GetCustomersPaginated")]
        public IActionResult GetCustomersPageByPage([FromQuery]QueryPagingParametersForCustomers inParameters)
        {
            List<Object> cusAccList = new List<Object>();
            //Pagination parameters
            int pageSize = 10;
            int currentPage = 0;
            string sortColumn = "";
            string sortOrder = "ASC";
            string searchFilter = "";

            int totalPage = 0;
            int startRecord = 0;
            int endRecord = 0;
            int totalRecords = 0;
            if (ModelState.IsValid) //if query params is valid load the data into pagination parameters
            {
                currentPage = Int32.Parse(inParameters.page_number.ToString());
                pageSize = Int32.Parse(inParameters.per_page.ToString());
                sortColumn = inParameters.sort_column;
                sortOrder = inParameters.sort_order;
                searchFilter = inParameters.search_filter;
            }
            else
            {
                currentPage = 1;
                pageSize = 10;
                sortColumn = "ACCOUNTNAME";
                sortOrder = "ASC";
                searchFilter = "";
            }
            if (currentPage == 1)//Calculate the page's first record's index
            {
                startRecord = 1;
            }
            else
            {
                startRecord = ((currentPage - 1) * pageSize) + 1;
            }
            //endRecord = pageSize * currentPage;
            try//Query the stored procedure in the database
            {
                DbCommand cmd = Database.Database.GetDbConnection().CreateCommand();

                cmd.Connection.Open();  //Open connection to database
                                        //Pass the SQL to the DbCommand type object, cmd.
                                        //Let the DbCommand type object cmd know that this is a stored procedure.
                cmd.CommandText = "dbo.uspGetCustomerPaginated";
                //Tell the DbCommand object, cmd that this is a stored procedure.
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                //Pass the page number value to the stored procedure's @pageNo parameter
                DbParameter parameter = cmd.CreateParameter();
                parameter.DbType = System.Data.DbType.Int32;
                parameter.ParameterName = "pageNo";
                parameter.Value = currentPage;
                cmd.Parameters.Add(parameter);
                //Pass the page size value to the stored procedure's @pageSize parameter
                parameter = cmd.CreateParameter();
                parameter.DbType = System.Data.DbType.Int32;
                parameter.ParameterName = "pageSize";
                parameter.Value = pageSize;
                cmd.Parameters.Add(parameter);
                //Pass the page size value to the stored procedure's @sortColumn parameter
                parameter = cmd.CreateParameter();
                parameter.DbType = System.Data.DbType.String;
                parameter.ParameterName = "sortColumn";
                parameter.Value = sortColumn;
                cmd.Parameters.Add(parameter);
                //Pass the page size value to the stored procedure's @sortORDER parameter
                parameter = cmd.CreateParameter();
                parameter.DbType = System.Data.DbType.String;
                parameter.ParameterName = "sortOrder";
                parameter.Value = sortOrder;
                cmd.Parameters.Add(parameter);
                if (!String.IsNullOrEmpty(searchFilter))//If search is not blank add the #customerName parameter to the query
                {
                    //Pass the page size value to the stored procedure's @customerName parameter
                    parameter = cmd.CreateParameter();
                    parameter.DbType = System.Data.DbType.String;
                    parameter.ParameterName = "customerName";
                    parameter.Value = searchFilter;
                    cmd.Parameters.Add(parameter);
                }

                DbDataReader dr = cmd.ExecuteReader();//This is the part where SQL is sent to DB
                if (dr.HasRows)//If the stored procedure results has records
                {
                    while (dr.Read())
                    {
                        //Get each column values
                        totalRecords = int.Parse(dr["TotalCount"].ToString());

                        int rowNum = int.Parse(dr["ROWNUM"].ToString());
                        int caId = int.Parse(dr["CustomerAccountId"].ToString());
                        string caAccountName = dr["AccountName"].ToString();
                        int caCommentCount = Database.CustomerAccountComments.Where(c => c.UpdatedAt.CompareTo(_appDateTimeService.GetCurrentDateTime().Date.AddDays(-3)) > 0).Count(c => c.CustomerAccountId == caId);
                        bool caIsVisible = bool.Parse(dr["IsVisible"].ToString());
                        string caCreatedBy = dr["CreatedBy"].ToString();
                        string caUpdatedBy = dr["UpdatedBy"].ToString();
                        DateTime caUpdatedAt = Convert.ToDateTime(dr["UpdatedAt"].ToString());

                        cusAccList.Add(new
                        {
                            rowNo = rowNum,
                            id = caId,
                            accountName = caAccountName,
                            comments = caCommentCount,
                            visibility = caIsVisible,
                            createdBy = caCreatedBy,
                            updatedBy = caUpdatedBy,
                            updatedAt = caUpdatedAt
                        });

                    }
                }
                cmd.Connection.Close();//Close connection
                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize); //Calculate total number of pages
                
                endRecord = startRecord + cusAccList.Count - 1; //Calculate index of last record on current page
                //Craft response object and return with Ok 200 response
                object result = new
                {
                    totalRecordCount = totalRecords,
                    totalCurrentPgRec = cusAccList.Count,
                    currentPage = currentPage,
                    totalPage = totalPage,
                    records = cusAccList,
                    from = startRecord,
                    to = endRecord

                };
                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            
        }

        // GET api/<controller>/5
        [Authorize("ADMIN")]
        [HttpGet("{id}")]
        public IActionResult GetCustomerInfo(int id)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).SingleOrDefault(c => c.CustomerAccountId == id);//Query Database for the specifc customer
                if (ca == null)
                {
                    return NotFound(new { message = "Customer could not be found" });//Throw Not found if customer is not found
                }
                else
                {//Cherry pick information to return
                    object cusAccObj = new
                    {
                        id = ca.CustomerAccountId,
                        accountName = ca.AccountName,
                        visibility = ca.IsVisible,
                        createdAt = ca.CreatedAt,
                        createdBy = ca.CreatedBy.FullName,
                        updatedAt = ca.UpdatedAt,
                        updatedBy = ca.UpdatedBy.FullName
                    };
                    return Ok(cusAccObj);//Return response with customer information
                }
            }catch(SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
        }

        [Authorize("ADMIN")]
        [HttpGet("GetCustomerAccountSummary/{id}")]
        public IActionResult GetCustomerAccountSummary(int id){
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id);//Query Database for the specifc customer
                if (ca == null)
                {
                    return NotFound(new { message = "Customer could not be found" });//Throw Not found if customer is not found
                }
                int accRateCount = Database.AccountRates.Where(ar => ar.CustomerAccountId == id).Count();//Retrieve the number of AccountRates records related to the customer

                int accRateATTCount = 0;
                if (accRateCount > 0)
                {
                    List<AccountRate> carList = Database.AccountRates.Where(r => r.CustomerAccountId == id).ToList();
                    foreach(AccountRate ar in carList)
                    {
                        accRateATTCount += Database.AccountTimeTable.Where(r => r.AccountRateId == ar.AccountRateId).Count();//Retrieve the number of AccountTimeTable records related to the customer
                    }
                }

                int accCommentCount = Database.CustomerAccountComments.Where(cac => cac.CustomerAccountId == id).Count();//Retrieve the number of CustomerAccountComments records related to the customer
                int accInstructorRelationCount = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Count();//Retrieve the number of InstructorAccounts records related to the customer
                //Craft response object and return with Ok 200 response
                object summary = new
                {
                    accountName = ca.AccountName,
                    accountRateCount = accRateCount,
                    accountTimeTableCount = accRateATTCount,
                    accountCommentCount = accCommentCount,
                    accountInstructorCount = accInstructorRelationCount
                   
                };
                return Ok(summary);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE

            }

        }

        

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("Create")]
        public IActionResult Post([FromForm] IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

            CustomerAccount ca = new CustomerAccount()
            {
                Comments = new List<CustomerAccountComment>(),
                AccountRates = new List<AccountRate>()
            };//Initailise a empty CustomerAccount object
            try
            {
                //Populating the empty CustomerAccount object
                ca.AccountName = data["accountName"];
                ca.IsVisible = Boolean.Parse(data["visibility"]);
                ca.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                ca.CreatedById = userId;
                ca.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                ca.UpdatedById = userId;

                if (!String.IsNullOrEmpty(data["accountComments"]))//Check if the comment portion of the webFormData is not null or empty
                {
                    //Populating the empty CustomerAccountComment object
                    CustomerAccountComment cac = new CustomerAccountComment();
                    cac.Comment = data["accountComments"].ToString().Trim();
                    cac.CustomerAccountId = ca.CustomerAccountId;
                    cac.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                    cac.CreatedById = userId;
                    cac.ParentId = null;
                    cac.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                    ca.Comments.Add(cac); //Add the populated CustomerAccountComments to the CustomerAccount object.
                }

                AccountRate ar = new AccountRate(); //Populating the empty AccountRate object
                ar.CustomerAccountId = ca.CustomerAccountId;
                ar.RatePerHour = decimal.Parse(data["accountRate"]);
                ar.EffectiveStartDate = DateTime.ParseExact(data["rateStartDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                ar.EffectiveEndDate = DateTime.ParseExact(data["rateEndDate"], "d/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                ar.CreatedAt = _appDateTimeService.GetCurrentDateTime();
                ar.CreatedById = userId;
                ar.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                ar.UpdatedById = userId;
                ca.AccountRates.Add(ar); //Add the populated AccountRate to the CustomerAccount object.


                Database.CustomerAccounts.Add(ca); //Add the Customer Account record to the database
                Database.SaveChanges(); 
                return Ok(new { message = "Successfully Registered " + ca.AccountName });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                string customMessage = "";
                if (ex.InnerException.Message.Contains("CustomerAccount_AccountName_UniqueConstraint") == true) //If Account name already exist in the database already, return Bad request specifying that the customer name already exist
                {
                    customMessage = data["accountName"] + " already exists as a customer account name.";
                }
                else //If the database is unable to save the record, return Bad request
                {
                    customMessage = "There are issues with the request.";
                }
                return BadRequest(new { message = customMessage });
            }
            

        }

        

        // PUT api/<controller>/5
        [Authorize("ADMIN")]
        [HttpPut("Update/{id}")]
        public IActionResult UpdateCustomerAccount(int id, [FromForm] IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value); //Get the current user logged in.
            CustomerAccount ca;
            try
            {
                ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id); //Find the CustomerAccount object with the specified customer ID
                //Update the account name and visibility in the found CustomerAccount object as well as last modified time and person
                ca.AccountName = data["accountName"].ToString();
                ca.IsVisible = bool.Parse(data["visibility"].ToString());
                ca.UpdatedById = userId;
                ca.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                //Update the record in the database
                Database.CustomerAccounts.Update(ca);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Updated " + ca.AccountName });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                string customMessage = "";
                if (ex.InnerException.Message.Contains("CustomerAccount_AccountName_UniqueConstraint") == true) //If Account name already exist in the database already, return Bad request specifying that the customer name already exist
                {
                    customMessage = data["accountName"] + " already exists as a customer account name.";
                }
                else //If the database is unable to save the record, return Bad request
                {
                    customMessage = "There are issues with the request.";
                }
                    return BadRequest(new { message = customMessage});
            }
            
        }

        // DELETE api/<controller>/5
        [Authorize("ADMIN")]
        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id); //Find the CustomerAccount object with the specified customer ID
                if (ca == null) //Return 404 Not Found if the CustomerAccountObject could not be found.
                {
                    return NotFound(new { message = "Customer could not be found." });
                }
                //Retrieve the records related to the CustomerAccount in the respective tables in the database.
                List<AccountRate> car = Database.AccountRates.Where(r => r.CustomerAccountId == id).ToList();
                List<CustomerAccountComment> cac = Database.CustomerAccountComments.Where(c => c.CustomerAccountId == id).ToList();
                List<InstructorAccount> cia = Database.InstructorAccounts.Where(a => a.CustomerAccountId == id).ToList();

                List<AccountTimeTable> catt = new List<AccountTimeTable>();
                if (car.Count > 0)
                {
                    foreach (AccountRate ar in car)
                    {
                        catt.AddRange(Database.AccountTimeTable.Where(r => r.AccountRateId == ar.AccountRateId).ToList());
                    }
                    Database.AccountTimeTable.RemoveRange(catt); //Remove all AccountTimeTables record related to the customer
                }

                //Remove all record from the respective tables related to the customer
                Database.AccountRates.RemoveRange(car);
                Database.CustomerAccountComments.RemoveRange(cac);
                Database.InstructorAccounts.RemoveRange(cia);
                Database.CustomerAccounts.Remove(ca);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Deleted: "+ca.AccountName }); //Return Ok 200 to tell the user the records has been successfully deleted.
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });//If any database related operation occur, return ISE
            }
        }

        public class QueryPagingParametersForCustomers
        {
            //Query parameters for pagination
            [BindRequired]
            public int page_number { get; set; }
            public int per_page { get; set; }
            public string sort_order { get; set; }
            public string sort_column { get; set; }
            public string search_filter { get; set; }

        }//end of QueryPagingParametersForCustomers class
    }
}
