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
            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

            try
            {
                List<CustomerAccountComment> cacList = Database.CustomerAccountComments.Include(c => c.CreatedBy).Where(c => c.CustomerAccountId == id).ToList(); //Retrieve all CustomerAccountComments records related to the customer from the database
                List<object> commentData = new List<object>(); //Response comment object list
                foreach (CustomerAccountComment cac in cacList) //Iterate through the list of CustomerAccountComments to cherry pick information to send to the client
                {
                    string FullName = "";
                    bool CreatedByCurrentUsr = false;
                    if (cac.CreatedById == userId) //If comment is created by current user
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
                        created_by_current_user = CreatedByCurrentUsr
                    });

                }
                return Ok(commentData); //Return Ok 200 response with comment data
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
        }

        // POST api/<controller>
        [Authorize("ADMIN")]
        [HttpPost]
        public IActionResult CreateCustomerComments([FromForm]IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

            CustomerAccountComment cac = new CustomerAccountComment(); //Initialise empty CustomerAccountComments object

            try
            {
                //Populate the empty CustomerAccountCommetns object
                cac.CustomerAccountId = int.Parse(data["customerAccountId"]);
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

                //Save the new CustomerAccountComments object to database.
                Database.CustomerAccountComments.Add(cac);
                Database.SaveChanges();
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            AppUser commentCreator = Database.AppUsers.Single(user => user.Id == cac.CreatedById); //Retireve the user object of the creator of current comment

            string FullName = "";
            bool CreatedByCurrentUsr = false;

            if (cac.CreatedById == userId) // If comment is created by current user
            {
                FullName = "You";
                CreatedByCurrentUsr = true;
            }
            else
            {
                FullName = commentCreator.FullName;
            }

            //Craft reponse object to return to the jQuery-comment library and send it.
            var commentObject = new
            {
                id = cac.CustomerAccountCommentId,
                parent = cac.ParentId,
                content = cac.Comment,
                fullname = FullName,
                modified = cac.UpdatedAt,
                created = cac.CreatedAt,
                creator = commentCreator.FullName,
                created_by_current_user = CreatedByCurrentUsr
            };

            return Ok(new { message = "Successfully added comments", comment = commentObject });
        }

        // PUT api/<controller>/5
        [Authorize("ADMIN")]
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromForm]IFormCollection data)
        {
            int userId = int.Parse(User.FindFirst("userid").Value); //Retireve the user id of the current logged in user

            CustomerAccountComment cac = Database.CustomerAccountComments.SingleOrDefault(c => c.CustomerAccountCommentId == id); //Initialise empty CustomerAccountComments object

            try
            {
                //Update the CustomerAccountCommetns object
                cac.Comment = data["content"].ToString();
                //if (String.IsNullOrEmpty(data["parent"]))
                //{
                //    cac.ParentId = null;
                //}
                //else
                //{
                //    cac.ParentId = int.Parse(data["parent"].ToString());
                //}
                cac.UpdatedAt = _appDateTimeService.GetCurrentDateTime();

                //Save the new CustomerAccountComments object to database.
                Database.CustomerAccountComments.Update(cac);
                Database.SaveChanges();
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            AppUser commentCreator = Database.AppUsers.Single(user => user.Id == cac.CreatedById); //Retireve the user object of the creator of current comment

            string FullName = "";
            bool CreatedByCurrentUsr = false;

            if (cac.CreatedById == userId) // If comment is created by current user
            {
                FullName = "You";
                CreatedByCurrentUsr = true;
            }
            else
            {
                FullName = commentCreator.FullName;
            }

            //Craft reponse object to return to the jQuery-comment library and send it.
            var commentObject = new
            {
                id = cac.CustomerAccountCommentId,
                parent = cac.ParentId,
                content = cac.Comment,
                fullname = FullName,
                modified = cac.UpdatedAt,
                created = cac.CreatedAt,
                creator = commentCreator.FullName,
                created_by_current_user = CreatedByCurrentUsr
            };

            return Ok(new { message = "Successfully updated comments", comment = commentObject });
        }

        // DELETE api/<controller>/5
        [Authorize("ADMIN")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {

            CustomerAccountComment cac = Database.CustomerAccountComments.SingleOrDefault(c => c.CustomerAccountCommentId == id); //Find the CustomerAccountComment record with the specified CustomreAccountCommentsId from the database

            try
            {
                //Delete the CustomerAccountComments object to database.
                Database.CustomerAccountComments.Remove(cac);
                Database.SaveChanges();
                return Ok(new { message = "Successfully deleted comments"});
            }
            catch (SqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Something wrong has occured. Please contact the administrators." }); //If any database related operation occur, return ISE
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
