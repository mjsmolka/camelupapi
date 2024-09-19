using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using CamelUpAutomation.Models.Configuration;
using CamelUpAutomation.Models.Users;

namespace CamelUpAutomation.Services
{
    public interface ICryptoService
    {
        string GenerateRandomString();
        string GenerateLowercaseString();
        string GenerateRandomString(int length);
        string GeneratePasswordHash(string email, string password);
        string GenerateEmailConfirmationCodeHash(string code, EmailConfirmationCodeAction action);
    }

    public class CryptoService : ICryptoService
    {
        IConfiguration _config;
        private string salt;
        private int slugLength;
        // generate a static class to generate a random string with letters and numbers 

        public CryptoService(IConfiguration config)
        {
            _config = config;
            salt = _config.GetValue<string>("CryptoSalt");
            slugLength = 20;
        }

        public string GenerateLowercaseString()
        {
            return GenerateRandomString(slugLength).ToLower();
        }

        public string GenerateRandomString()
        {
            return GenerateRandomString(slugLength);
        }

        public string GenerateRandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                             .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string GeneratePasswordHash(string email, string password)
        {
            return this.HashString(email.ToLower().Trim() + password);
        }

        public string GenerateEmailConfirmationCodeHash(string code, EmailConfirmationCodeAction action)
        {
            return this.HashString(code + action.ToString());
        }

        private string HashString(string input)
        {
            input += salt;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private class CryptoSettings
        {
            public string Salt { get; set; }
            public int SlugLength { get; set; }
        }
    }
}
