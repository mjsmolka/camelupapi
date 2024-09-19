using System;
namespace CamelUpAutomation.Auth;

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
	ServiceResult<string> VerifyJWTToken(string token, bool emailVerified = true);
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

		var getUserResult = await _userService.GetUserByEmail(email);
		if (getUserResult.IsSuccessful)
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
			new Claim("emailVerified", user.EmailConfirmed.ToString()),
			new Claim("userId", user.Id.ToString()),
			new Claim("username", user.UserName.ToString())
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

	public ServiceResult<string> VerifyJWTToken(string token, bool emailVerified = true)
	{
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.GetValue<string>("JWTSecretKey"));
		try
		{
			var result = tokenHandler.ValidateToken(token, new TokenValidationParameters
			{
				ValidIssuer = _config.GetValue<string>("JWTIssuer"),
				ValidAudience = _config.GetValue<string>("JWTAudience"),
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetValue<string>("JWTSecretKey"))),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = false,
				ValidateIssuerSigningKey = true
			}, out SecurityToken validatedToken);
			var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
			// get the userId from the token claims
			var userId = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "userId").Value;
			var isEmailVerified = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "emailVerified").Value;
			if (emailVerified && isEmailVerified == "False")
			{
                return ServiceResult<string>.FailedResult("Email not verified", ServiceResponseCode.Forbidden);
            }
			return ServiceResult<string>.SuccessfulResult(userId);
		} catch (Exception e)
		{
			return ServiceResult<string>.FailedResult("Invalid Token", ServiceResponseCode.Forbidden);
		}
		
	}

	private async Task<ServiceResult<EmailConfirmationCode>> VerifyEmailAddressCode(string code)
	{
		var emailConfirmationCode = await _emailConfirmationCodeService.GetEmailConfirmationCode(code, EmailConfirmationCodeAction.ConfirmEmail);
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
		string actionUrl = _config.GetValue<string>("ActionUrl") + $"?code={code}";
		WelcomeEmailData welcomeEmailData = new WelcomeEmailData
		{
			Email = user.Email,
			Name = user.UserName,
			ActionUrl = actionUrl,
		};
		string emailTemplate = _emailService.GetEmailTemplate("VerifyEmail", welcomeEmailData);
		MailData mailData = new MailData(
		  new List<string> { user.Email },
		  "Confirm Autocamel password",
		  emailTemplate
		);
		await _emailService.SendAsync(mailData, new CancellationToken());
		return true;
	}

	private async Task<bool> SendPasswordResetEmail(User user)
	{
		string token = await GetChangePasswordCode(user);
		string actionUrl = _config.GetValue<string>("ActionUrl") + $"?code={token}&action=reset";
		PasswordResetEmailData passwordResetEmailData = new PasswordResetEmailData
		{
			Email = user.Email,
			ActionUrl = actionUrl,
		};
		string emailBody = _emailService.GetEmailTemplate("ResetPassword", passwordResetEmailData);
		MailData mailData = new MailData(
		  new List<string> { user.Email },
		  "Reset AutoCamel Password",
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
