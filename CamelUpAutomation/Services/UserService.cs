using CamelUpAutomation.Models.ReturnObjects;
using CamelUpAutomation.Models.Users;
using CamelUpAutomation.Repos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{
	 public interface IUserService
	{
		Task<ServiceResult<User>> CreateUser(string email, string password, string username);
		Task<ServiceResult> DeleteUserByEmail(string email);
		Task<ServiceResult> DeleteUserById(string id);
		Task<ServiceResult<User>> GetUser(string id);
		Task<ServiceResult> UpdateUser(User user);
		Task<ServiceResult<User>> GetUserByEmail(string email);
	}

	public class UserService : IUserService
	{

		private readonly IUserRepo _userRepo;
		private readonly ICryptoService _cryptoService;

		public UserService(IUserRepo userRepo, ICryptoService cryptoService)
		{
			_userRepo = userRepo;
			_cryptoService = cryptoService;
		}

		public async Task<ServiceResult<User>> CreateUser(string email, string password, string username)
		{
			 string passwordHash = _cryptoService.GeneratePasswordHash(email, password);
			 User user = new User()
			 {
				Email = email,
				SecurityStamp = Guid.NewGuid().ToString(),
				UserName = username,
				PasswordHash = passwordHash
			 };
			 string userId = _cryptoService.GenerateLowercaseString();
			 user.Id = userId;
			 user.userId = userId; // this is the same as the id. Cosmos db requires the first letter to be lowercase
		     user.id = userId;
			 await this._userRepo.AddUser(user);
			 return ServiceResult<User>.SuccessfulResult(user);
		}

		public async Task<ServiceResult> DeleteUserByEmail(string email)
		{
			User user = await _userRepo.GetUser(email);
			if (user != null)
			{
                await _userRepo.DeleteUser(user.Id);
            }
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> DeleteUserById(string id)
		{
			await _userRepo.DeleteUser(id);
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult<User>> GetUser(string id)
		{
			var user = await _userRepo.GetUser(id);
			return ServiceResult<User>.SuccessfulResult(user);
		}

		public async Task<ServiceResult> UpdateUser(User user)
		{
            await _userRepo.UpdateUser(user);
			return ServiceResult.SuccessfulResult();
        }

		public async Task<ServiceResult<User>> GetUserByEmail(string email)
		{
			var user = await _userRepo.GetUserEmail(email);
			if (user != null)
			{
                return ServiceResult<User>.SuccessfulResult(user);
            }
			return ServiceResult<User>.FailedResult("User not found", ServiceResponseCode.NotFound);
		}

		public Task<List<User>> GetUsers(int skip = 0, int take = 100)
		{
			throw new NotImplementedException();
		}
    }
}
