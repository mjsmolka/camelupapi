using CamelUpAutomation.Models.Users;
using CamelUpAutomation.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{

    public interface IEmailConfirmationCodeService
    {
        Task<string> AddEmailConfirmationCode(string userId, EmailConfirmationCodeAction action);
        Task<EmailConfirmationCode> GetEmailConfirmationCode(string code, EmailConfirmationCodeAction action);
    }
    public class EmailConfirmationCodeService : IEmailConfirmationCodeService
    {

        private readonly IEmailConfirmationCodeRepo _emailConfirmationCodeRepo;
        private readonly ICryptoService _cryptoService;

        public EmailConfirmationCodeService(IEmailConfirmationCodeRepo emailConfirmationCodeRepo, ICryptoService cryptoService)
        {
            _emailConfirmationCodeRepo = emailConfirmationCodeRepo;
            _cryptoService = cryptoService;
        }

        public async Task<string> AddEmailConfirmationCode(string userId, EmailConfirmationCodeAction action)
        {
            string code = _cryptoService.GenerateRandomString();
            string codeHash = _cryptoService.GenerateEmailConfirmationCodeHash(code, action);
            var emailConfirmationCode = new EmailConfirmationCode
            {
                id = _cryptoService.GenerateRandomString(),
                UserId =  userId,
                ExperationTime = DateTime.Now.AddHours(4),
                CodeHash = codeHash,
                Action = action
            };
            await _emailConfirmationCodeRepo.AddEmailConfirmationCodeAsync(emailConfirmationCode);
            return code;
        }

        public Task<EmailConfirmationCode> GetEmailConfirmationCode(string code, EmailConfirmationCodeAction action)
        {
            string codeHash = _cryptoService.GenerateEmailConfirmationCodeHash(code, action);
            var emailConfirmationCode = _emailConfirmationCodeRepo.GetEmailConfirmationCodeAsync(codeHash, action);
            if (emailConfirmationCode != null)
            {
                return emailConfirmationCode;
            }
            return null;
        }
    }
}
