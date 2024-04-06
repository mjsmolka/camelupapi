using System;
namespace CamelUpAutomation.Auth;

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CamelUpAutomation.Models.Email;
using CamelUpAutomation.Models.ReturnObjects;
using CamelUpAutomation.Models.Users;
using CamelUpAutomation.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public interface IAuthService
{
	Task<ServiceResult<string>> Authenticate(string email, string password);
	Task<ServiceResult> Register(string email, string username, string password);
	Task<ServiceResult> ResetPassword(string email);
	Task<ServiceResult> DeleteUser(string email);
	Task<ServiceResult> VerifyEmailAddress(string code);

	Task<ServiceResult> UpdateUserPassword(string code, string newPassword);
}

public class AuthService : IAuthService
{
	private readonly IConfiguration _config;
	private readonly IEmailService _emailService;
	private readonly RoleManager<IdentityRole> _roleManager;
	private readonly IUserService _userService;
	private readonly ICryptoService _cryptoService;
	private readonly IEmailConfirmationCodeService _emailConfirmationCodeService;

	public AuthService(
		IConfiguration config,
		UserManager<User> userManager,
		RoleManager<IdentityRole> roleManager,
		IEmailService emailService,
		IUserService userService,
		ICryptoService cryptoService,
		IEmailConfirmationCodeService emailConfirmationCodeService
	)
	{
		_emailService = emailService;
		_config = config;
		_roleManager = roleManager;
		_userService = userService;
		_cryptoService = cryptoService;
		_emailConfirmationCodeService = emailConfirmationCodeService;
	}

	public async Task<ServiceResult<string>> Authenticate(string email, string password)
	{
		var userResponse = await _userService.GetUserByEmail(email);
		if (!userResponse.IsSuccessful)
		{
			return ServiceResult<string>.ConvertResult(userResponse);
		}
		var user = userResponse.Result;
		string hashedPassword = _cryptoService.GeneratePasswordHash(email, password);
		if (user.PasswordHash != hashedPassword)
		{
			return ServiceResult<string>.FailedResult("Invalid Password", ServiceResponseCode.Forbidden);
			
		}
		return GenerateJWTToken(user);
	}

	public async Task<ServiceResult> Register(string email, string username, string password)
	{

		var emailExists = await _userService.GetUserByEmail(email);
		if (emailExists != null)
		{
			return ServiceResult.FailedResult("User already exists with this email", ServiceResponseCode.Forbidden);
		}
	   
		ServiceResult<User> userResult = await _userService.CreateUser(email, password, username);
		await SendEmailVerificationEmail(userResult.Result);
		return ServiceResult.SuccessfulResult();
	}
	public async Task<ServiceResult> ResetPassword(string email) { 
		var userResponse = await _userService.GetUserByEmail(email);
		if (!userResponse.IsSuccessful)
		{
			return userResponse;
		}
		await SendPasswordResetEmail(userResponse.Result);
		return ServiceResult.SuccessfulResult();
	}

	public async Task<ServiceResult> DeleteUser(string email)
	{
		await _userService.DeleteUserByEmail(email);
		return ServiceResult.SuccessfulResult();;
	}

	public async Task<ServiceResult> VerifyEmailAddress(string code)
	{
		ServiceResult<EmailConfirmationCode> emailConfirmationCodeResponse = await VerifyEmailAddressCode(code);
		if (!emailConfirmationCodeResponse.IsSuccessful)
		{
            return emailConfirmationCodeResponse;
        }
		ServiceResult<User> userResult = await _userService.GetUser(emailConfirmationCodeResponse.Result.UserId);
		if (!userResult.IsSuccessful)
		{
            return userResult;
        }
		var user = userResult.Result;
		user.EmailConfirmed = true;
		await _userService.UpdateUser(user);
		return ServiceResult.SuccessfulResult();
	}

	public async Task<ServiceResult> UpdateUserPassword(string code, string newPassword)
	{
		ServiceResult<EmailConfirmationCode> emailConfirmationCodeResponse = await VerifyResetPasswordCode(code);
		if (!emailConfirmationCodeResponse.IsSuccessful)
		{
            return emailConfirmationCodeResponse;
        }


		ServiceResult<User> getUserResult = await _userService.GetUser(emailConfirmationCodeResponse.Result.UserId);
		if (!getUserResult.IsSuccessful)
		{
			return getUserResult;
		}
		var user = getUserResult.Result;
		user.PasswordHash = _cryptoService.GeneratePasswordHash(user.Email, newPassword);
		await _userService.UpdateUser(user);
		return ServiceResult.SuccessfulResult();
	}

	private ServiceResult<string> GenerateJWTToken(User user)
	{
		var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetValue<string>("JWTSecretKey")));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Email),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim("emailVerified", user.EmailConfirmed.ToString())
		};
		var token = new JwtSecurityToken(
			issuer: _config.GetValue<string>("JWTIssuer"),
			audience: _config.GetValue<string>("JWTAudience"),
			claims: claims,
			expires: DateTime.Now.AddDays(1),
			signingCredentials: credentials
		);
		string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
		return ServiceResult<string>.SuccessfulResult(tokenString);
	}

	private async Task<ServiceResult<EmailConfirmationCode>> VerifyEmailAddressCode(string code)
	{
		EmailConfirmationCode emailConfirmationCode = await _emailConfirmationCodeService.GetEmailConfirmationCode(code, EmailConfirmationCodeAction.ConfirmEmail);
		if (emailConfirmationCode == null)
		{
			return ServiceResult<EmailConfirmationCode>.FailedResult("Invalid code", ServiceResponseCode.Forbidden);
		}
		return ServiceResult<EmailConfirmationCode>.SuccessfulResult(emailConfirmationCode);
	}

	private async Task<ServiceResult<EmailConfirmationCode>> VerifyResetPasswordCode(string code)
	{
		EmailConfirmationCode emailConfirmationCode = await _emailConfirmationCodeService.GetEmailConfirmationCode(code, EmailConfirmationCodeAction.ResetPassword);
		if (emailConfirmationCode == null)
		{
			return ServiceResult<EmailConfirmationCode>.FailedResult("Invalid code", ServiceResponseCode.Forbidden);
		}
		return ServiceResult<EmailConfirmationCode>.SuccessfulResult(emailConfirmationCode);
	}

	private async Task<bool> SendEmailVerificationEmail(User user)
	{
		var code = await GetEamilVerificationCode(user);
		code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
		string actionUrl = _config.GetSection("ActionUrls").GetValue("VerifyEmail", "localhost") + $"{code}/{user.Email}";
		WelcomeEmailData welcomeEmailData = new WelcomeEmailData
		{
			Email = user.Email,
			Name = user.UserName,
			ActionUrl = actionUrl,
		};
		string emailTemplate = _emailService.GetEmailTemplate("VerifyEmail", welcomeEmailData);
		MailData mailData = new MailData(
		  new List<string> { user.Email },
		  "Welcome to the MailKit Demo",
		  emailTemplate
		);
		await _emailService.SendAsync(mailData, new CancellationToken());
		return true;
	}

	private async Task<bool> SendPasswordResetEmail(User user)
	{
		string token = await GetChangePasswordCode(user);
		string actionUrl = _config.GetSection("ActionUrls").GetValue("VerifyEmail", "localhost") + $"{token}/{user.Email}";
		PasswordResetEmailData passwordResetEmailData = new PasswordResetEmailData
		{
			Email = user.Email,
			ActionUrl = actionUrl,
		};
		string emailBody = _emailService.GetEmailTemplate("ResetPassword", passwordResetEmailData);
		MailData mailData = new MailData(
		  new List<string> { "martin@skismolka.com" },
		  "Welcome to the MailKit Demo",
		  emailBody
		);
		await _emailService.SendAsync(mailData, new CancellationToken());

		return true;
	}
	private async Task<string> GetEamilVerificationCode(User user)
	{
		return await _emailConfirmationCodeService.AddEmailConfirmationCode(user.Id, EmailConfirmationCodeAction.ConfirmEmail);
	}

	private async Task<string> GetChangePasswordCode(User user)
	{
		return await _emailConfirmationCodeService.AddEmailConfirmationCode(user.Id, EmailConfirmationCodeAction.ResetPassword);
	}
}
