using System;
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

            int pageSize = 10;
            int currentPage = 0;
            string sortColumn = "";
            string sortOrder = "ASC";
            string searchFilter = "";

            int totalPage = 0;
            int startRecord = 0;
            int endRecord = 0;
            int totalRecords = 0;
            if (ModelState.IsValid)
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
            if (currentPage == 1)
            {
                startRecord = 1;
            }
            else
            {
                startRecord = ((currentPage - 1) * pageSize) + 1;
            }
            //endRecord = pageSize * currentPage;
            try
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
                if (!String.IsNullOrEmpty(searchFilter))
                {
                    //Pass the page size value to the stored procedure's @customerName parameter
                    parameter = cmd.CreateParameter();
                    parameter.DbType = System.Data.DbType.String;
                    parameter.ParameterName = "customerName";
                    parameter.Value = searchFilter;
                    cmd.Parameters.Add(parameter);
                }

                DbDataReader dr = cmd.ExecuteReader();//This is the part where SQL is sent to DB
                if (dr.HasRows)
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
                cmd.Connection.Close();
                totalPage = (int)Math.Ceiling((double)totalRecords / pageSize);

                endRecord = startRecord + cusAccList.Count - 1;
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
            
        }



        // GET api/<controller>/5
        [Authorize("ADMIN")]
        [HttpGet("{id}")]
        public IActionResult GetCustomerInfo(int id)
        {
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.Include(c => c.CreatedBy).Include(c => c.UpdatedBy).SingleOrDefault(c => c.CustomerAccountId == id);
                if (ca == null)
                {
                    return NotFound(new { message = "Customer could not be found" });
                }
                else
                {
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
                    return Ok(cusAccObj);
                }
            }catch(SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
        }

        [Authorize("ADMIN")]
        [HttpGet("GetCustomerAccountSummary/{id}")]
        public IActionResult GetCustomerAccountSummary(int id){
            try
            {
                CustomerAccount ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id);
                if(ca == null)
                {
                    return NotFound(new { message = "Customer could not be found" });
                }
                int accRateCount = Database.AccountRates.Where(ar => ar.CustomerAccountId == id).Count();
                int accCommentCount = Database.CustomerAccountComments.Where(cac => cac.CustomerAccountId == id).Count();
                int accInstructorRelationCount = Database.InstructorAccounts.Where(ia => ia.CustomerAccountId == id).Count();
                object summary = new
                {
                    accountName = ca.AccountName,
                    accountRateCount = accRateCount,
                    accountCommentCount = accCommentCount,
                    accountInstructorCount = accInstructorRelationCount
                };
                return Ok(summary);
            } catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });

            }

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
                AccountRates = new List<AccountRate>()
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
                return Ok(new { message = "Successfully Registered " + ca.AccountName });
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
        [HttpPut("Update/{id}")]
        public IActionResult UpdateCustomerAccount(int id, [FromForm] IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value);
            CustomerAccount ca;
            try
            {
                ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id);
                ca.AccountName = data["accountName"].ToString();
                ca.IsVisible = bool.Parse(data["visibility"].ToString());
                ca.UpdatedById = userId;
                ca.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
            
                Database.CustomerAccounts.Update(ca);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Updated " + ca.AccountName });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
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
                CustomerAccount ca = Database.CustomerAccounts.SingleOrDefault(c => c.CustomerAccountId == id);
               if(ca == null)
                {
                    return NotFound(new { message = "Customer could not be found." });
                }

                List<AccountRate> car = Database.AccountRates.Where(r => r.CustomerAccountId == id).ToList();
                List<CustomerAccountComment> cac = Database.CustomerAccountComments.Where(c => c.CustomerAccountId == id).ToList();
                List<InstructorAccount> cia = Database.InstructorAccounts.Where(a => a.CustomerAccountId == id).ToList();

                Database.AccountRates.RemoveRange(car);
                Database.CustomerAccountComments.RemoveRange(cac);
                Database.InstructorAccounts.RemoveRange(cia);
                Database.CustomerAccounts.Remove(ca);
                Database.SaveChanges();
                return Ok(new { message = "Successfully Deleted: "+ca.AccountName });
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
        }

        public class QueryPagingParametersForCustomers
        {
            [BindRequired]
            public int page_number { get; set; }
            public int per_page { get; set; }
            public string sort_order { get; set; }
            public string sort_column { get; set; }
            public string search_filter { get; set; }

        }//end of QueryPagingParametersForCustomers class
    }
}
