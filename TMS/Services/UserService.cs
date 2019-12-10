using System;
using System.Collections.Generic;
using System.Linq;
using TMS.Models;
using TMS.Helpers;

using Microsoft.EntityFrameworkCore;
using TMS.Data;
using System.Threading.Tasks;

namespace TMS.Services
{
    public interface IUserService
    {
        Task<AppUser> AuthenticateAsync(string inUserId, string inPassword);
		Task<List<AppUser>> GetAllUsersAsync();
        Task<List<AppUser>> GetAllUsersByRoleIdAsync(int roleId);
        Task<AppUser> GetUserByIdAsync(int id);
		Task<int> UpdateAsync(AppUser user, string password = null);
		Task<AppUser> CreateAsync(AppUser user, string password);
        Task<int> DeleteAsync(int id);
    }

	public class UserService : IUserService
	{
		private IAppDateTimeService _appDateTimeService;
		public ApplicationDbContext Database { get; }

		public UserService(ApplicationDbContext context, 
		IAppDateTimeService appDateTimeService)
        {//Intialize the Database property and the member variable(s)
            Database = context;
			_appDateTimeService = appDateTimeService;
        }
		public async Task<int> UpdateAsync(AppUser user, string password = null){
			int result = 0;
			AppUser tempUser = new AppUser();
			tempUser = await Database.AppUsers.FindAsync(user.Id);
			if (tempUser == null)
			{
				throw new AppException("User not found");
			}
			if (user.UserName != user.UserName)
			{
				// username has changed so check if the new username is already taken
				if (await Database.AppUsers.AnyAsync(x => x.UserName == user.UserName))
				{
					throw new AppException("User name " + user.UserName + " is already taken");
				}
			}
			//Update user properties
			tempUser.FullName = user.FullName;
			tempUser.UserName = user.UserName;
			tempUser.RoleId = user.RoleId;
			tempUser.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
			//Update password if it was entered
			if (!string.IsNullOrWhiteSpace(password))
			{
				byte[] passwordHash, passwordSalt;
				CreatePasswordHash(password, out passwordHash, out passwordSalt);

				tempUser.PasswordHash = passwordHash;
				tempUser.PasswordSalt = passwordSalt;
			}
			try
			{
				Database.AppUsers.Update(tempUser);
				result = await Database.SaveChangesAsync();
				return result;
			}
			catch
			{
				throw new AppException("Update operation encountered error.");
			}
		}

		public async Task<AppUser> AuthenticateAsync(string inUserName, string inPassword)
        {
            if (string.IsNullOrEmpty(inUserName) || string.IsNullOrEmpty(inPassword))
                return null;
            //Check whether there is a matching user name information first.
            //Then, the subsequent code will verify the password by calling
            //the VefiryPasswordHash method
            var user = await Database.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(x => x.UserName == inUserName);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(inPassword, user.PasswordHash, user.PasswordSalt))
                return null;

            // authentication successful
            return user;
        }
	
		public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await Database.AppUsers.Include(user => user.Role).ToListAsync();
        }
        public async Task<List<AppUser>> GetAllUsersByRoleIdAsync(int roleId)
        {
            return await Database.AppUsers.Where(user=> user.RoleId == roleId)
			.Include(user => user.Role)
			.AsNoTracking()
			.ToListAsync();
        }
        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await Database.AppUsers.FindAsync(id);
        }

        public async Task<AppUser> CreateAsync(AppUser user, string password)
        {
            // validation to check if the password is empty or spaces only.
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");
            // If the user name (email) already exists, raise an exception
            // so that the Web API controller class code can capture the error and
            // send back a JSON response to the client side.
            if (await Database.AppUsers.AnyAsync(appUser => appUser.UserName == user.UserName))
                throw new AppException("Username " + user.UserName + " is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
			user.CreatedAt = _appDateTimeService.GetCurrentDateTime();
			user.UpdatedAt = _appDateTimeService.GetCurrentDateTime();
            Database.AppUsers.Add(user);
            await Database.SaveChangesAsync();
            return user;
        }



        public async Task<int> DeleteAsync(int id)
        {
			int result = 0;
            var user = await Database.AppUsers.FindAsync(id);
            if (user != null)
            {
                try
                {
                    Database.AppUsers.Remove(user);
                    result = await Database.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.ToUpper().Contains("REFERENCE CONSTRAINT"))
                    {
                        throw new AppException("Unable to delete user record. The user information might have been linked to other information. ");
                    }
                    else
                    {
                        throw new AppException("Unable to delete user record.");
                    }
                }
            }
			return result;
			}

        // private helper methods

        private static void CreatePasswordHash(string inPassword, out byte[] inPasswordHash, out byte[] inPasswordSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            //The password is hashed with a new random salt.
            //https://crackstation.net/hashing-security.htm
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                inPasswordSalt = hmac.Key;
                inPasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
            }
        }

        private static bool VerifyPasswordHash(string inPassword, byte[] inStoredHash, byte[] inStoredSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (inStoredHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (inStoredSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(inStoredSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != inStoredHash[i]) return false;
                }
            }

            return true;
        }


	}
}