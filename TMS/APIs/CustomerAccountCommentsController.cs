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
    public class CustomerAccountCommentsController : Controller
    {
        private IAppDateTimeService _appDateTimeService;
        public ApplicationDbContext Database { get; }
        //The following constructor code pattern is required for every Web API
        //controller class.
        public CustomerAccountCommentsController(IAppDateTimeService appDateTimeService, ApplicationDbContext database)
        {
            Database = database;
            _appDateTimeService = appDateTimeService;
        }

        // GET api/<controller>/5
        [Authorize("ADMIN")]
        [HttpGet("{id}")]
        public IActionResult GetCustomerComments(int id)
        {
            int userId = int.Parse(User.FindFirst("userid").Value);

            try
            {
                List<CustomerAccountComment> cacList = Database.CustomerAccountComments.Include(c => c.CreatedBy).Where(c => c.CustomerAccountId == id).ToList();
                List<object> commentData = new List<object>();
                foreach (CustomerAccountComment cac in cacList)
                {
                    string FullName = "";
                    bool CreatedByCurrentUsr = false;
                    if (cac.CreatedById == userId)
                    {
                        FullName = "You";
                        CreatedByCurrentUsr = true;
                    }
                    else
                    {
                        FullName = cac.CreatedBy.FullName;
                    }
                    commentData.Add(new
                    {
                        id = cac.CustomerAccountCommentId,
                        parent = cac.ParentId,
                        content = cac.Comment,
                        fullname = FullName,
                        modified = cac.UpdatedAt,
                        created = cac.CreatedAt,
                        creator = cac.CreatedBy.FullName,
                        createdByCurrentUser = CreatedByCurrentUsr
                    });

                }
                return Ok(commentData);
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
        }

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost("{id}")]
        public IActionResult CreateCustomerComments(int id, [FromForm]IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value);

            CustomerAccountComment cac = new CustomerAccountComment();

            try
            {
                cac.CustomerAccountId = id;
                cac.Comment = data["content"].ToString();
                if (String.IsNullOrEmpty(data["parent"]))
                {
                    cac.ParentId = null;
                }
                else
                {
                    cac.ParentId = int.Parse(data["parent"].ToString());
                }
                cac.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
                cac.CreatedById = userId;
                cac.CreatedAt = _appDateTimeService.GetCurrentDateTime();

                Database.CustomerAccountComments.Add(cac);
                Database.SaveChanges();
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            AppUser commentCreator = Database.AppUsers.Single(user => user.Id == cac.CreatedById);

            string FullName = "";
            bool CreatedByCurrentUsr = false;

            if (cac.CreatedById == userId)
            {
                FullName = "You";
                CreatedByCurrentUsr = true;
            }
            else
            {
                FullName = commentCreator.FullName;
            }


            var commentObject = new
            {
                id = cac.CustomerAccountCommentId,
                parent = cac.ParentId,
                content = cac.Comment,
                fullname = FullName,
                modified = cac.UpdatedAt,
                created = cac.CreatedAt,
                creator = commentCreator.FullName,
                createdByCurrentUser = CreatedByCurrentUsr
            };

            return Ok(new { message = "Successfully added comments", comment = commentObject });
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
