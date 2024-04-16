using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CamelUpAutomation.DTOs.Auth;
using CamelUpAutomation.Auth;
using CamelUpAutomation.Services;
using CamelUpAutomation.Models.ReturnObjects;
using System.Web.Http;

namespace CamelUpAutomation.Functions
{
	public class AuthenticationFunction
	{

		private readonly IAuthService _authService;
		private readonly IValidatorService _validatorService;
		public AuthenticationFunction(
			IAuthService authService,
			IValidatorService validatorService
		)
		{
			_authService = authService;
			_validatorService = validatorService;
		}

		const string prefix = "auth";

		[FunctionName(AzureFunctionNames.AuthLogin)]
		public async Task<IActionResult> AuthLogin(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = prefix + "/login")] HttpRequest req,
			ILogger log)
		{
			try
			{
				log.LogInformation("C# HTTP trigger function processed a request: " + AzureFunctionNames.AuthLogin);

				ServiceResult<LoginDto> validateResult = await _validatorService.ValidateDto<LoginDto>(req);
				if (!validateResult.IsSuccessful)
				{
					return validateResult.ActionResult;
				}
				var loginDto = validateResult.Result;
				var loginResult = await _authService.Authenticate(loginDto.Email, loginDto.Password);
				return loginResult.ActionResult;
			} catch (Exception e) { 
				log.LogError(e.Message);
				return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
        }

		[FunctionName(AzureFunctionNames.AuthRegister)]
		public async Task<IActionResult> AuthRegister(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = prefix + "/register")] HttpRequest req,
			ILogger log)
		{
			try
			{
				log.LogInformation("C# HTTP trigger function processed a request: " + AzureFunctionNames.AuthConfirmEmail);
				ServiceResult<RegisterDto> validateResult = await _validatorService.ValidateDto<RegisterDto>(req);
				if (!validateResult.IsSuccessful)
				{
					return validateResult.ActionResult;
				} 

				RegisterDto dto = validateResult.Result;
				var userResponse = await _authService.Register(dto.Email, dto.UserName, dto.Password);
				return userResponse.ActionResult;
			} catch (Exception e)
			{
				log.LogError(e.Message);
				return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}

		[FunctionName(AzureFunctionNames.AuthConfirmEmail)]
		public async Task<IActionResult> AuthConfirmEmail(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = prefix + "/confirm-email")] HttpRequest req,
			ILogger log)
		{
			try
			{
				log.LogInformation("C# HTTP trigger function processed a request: " + AzureFunctionNames.AuthConfirmEmail);
				ServiceResult<VerifyEmailDto> validateResult = await _validatorService.ValidateDto<VerifyEmailDto>(req);
				if (!validateResult.IsSuccessful)
				{
					return validateResult.ActionResult;
				} 

				VerifyEmailDto dto = validateResult.Result;
				var verifyEmailResponse = await _authService.VerifyEmailAddress(dto.Code);
				return verifyEmailResponse.ActionResult;
			} catch (Exception e)
			{
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}

		[FunctionName(AzureFunctionNames.AuthChangePassword)]
		public async Task<IActionResult> AuthChangePassword(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = prefix + "/change-password")] HttpRequest req,
			ILogger log)
		{
			try
			{
				log.LogInformation("C# HTTP trigger function processed a request: " + AzureFunctionNames.AuthChangePassword);
				ServiceResult<ChangePasswordDto> validateResult = await _validatorService.ValidateDto<ChangePasswordDto>(req);
				if (!validateResult.IsSuccessful)
				{
					return validateResult.ActionResult;
				} 

				ChangePasswordDto dto = validateResult.Result;
				var verifyEmailResponse = await _authService.UpdateUserPassword(dto.Code, dto.Password);
				return verifyEmailResponse.ActionResult;
			} catch (Exception e)
			{
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}

		[FunctionName(AzureFunctionNames.AuthDeleteAccount)]
		public async Task<IActionResult> AuthDeleteAccount(
			[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = prefix + "/delete-account")] HttpRequest req,
			ILogger log)
		{
			try
			{
				log.LogInformation("C# HTTP trigger function processed a request: " + AzureFunctionNames.AuthDeleteAccount);
				req.Headers.TryGetValue("token", out var token);
				ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token, false);
				if (!tokenResult.IsSuccessful)
				{
                    return tokenResult.ActionResult;
                }
				var response = await _authService.DeleteUser(tokenResult.Result);
				return response.ActionResult;
			} catch (Exception e)
			{
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}

		[FunctionName("TestPassword")]
		public async Task<IActionResult> TestPassword(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = prefix + "/test-password")] HttpRequest req,
			ILogger log)
		{
			try
			{
				// get the value from the header named 'token'
				// generate a line to grab values from the HttpRequest header 
				req.Headers.TryGetValue("token", out var token);

				Console.WriteLine(token);

				log.LogInformation("C# HTTP trigger function processed a request: " + "Test Decoding");
				ServiceResult<LoginDto> validateResult = await _validatorService.ValidateDto<LoginDto>(req);
				if (!validateResult.IsSuccessful)
				{
					return validateResult.ActionResult;
				} 
				var response = _authService.VerifyJWTToken(validateResult.Result.Password);
				return response.ActionResult;
			} catch (Exception e)
			{
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}
	}
}
