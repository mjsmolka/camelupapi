using CamelUpAutomation.Enums;
using CamelUpAutomation.Models.Users;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; 

namespace CamelUpAutomation.Repos
{
   

    public interface IEmailConfirmationCodeRepo
    {
        Task AddEmailConfirmationCodeAsync(EmailConfirmationCode emailConfirmationCode);
        Task<EmailConfirmationCode> GetEmailConfirmationCodeAsync(string codeHash, EmailConfirmationCodeAction action);
    }

    public class EmailConfirmationCodeRepo : IEmailConfirmationCodeRepo
    {
        private readonly Container _emailConfirmationCodeContainer;
        public EmailConfirmationCodeRepo(IClientFactory clientFactory) {
            _emailConfirmationCodeContainer = clientFactory.GetContainer(ContainerNames.EmailConfirmationCodes);
        }


        public async Task AddEmailConfirmationCodeAsync(EmailConfirmationCode emailConfirmationCode)
        {
            await _emailConfirmationCodeContainer.CreateItemAsync(emailConfirmationCode, new PartitionKey(emailConfirmationCode.CodeHash));
        }

        public async Task<EmailConfirmationCode> GetEmailConfirmationCodeAsync(string codeHash, EmailConfirmationCodeAction action)
        {  
            var iterator =  _emailConfirmationCodeContainer.GetItemLinqQueryable<EmailConfirmationCode>()
                            .Where(c => c.CodeHash == codeHash && c.Action == action).ToFeedIterator();
            var emailConfirmationCode = await iterator.ReadNextAsync();

            var confirmationCode = emailConfirmationCode.FirstOrDefault();
            if (confirmationCode != null)
            {
                await DeleteEmailConfirmationCodeAsync(codeHash);
            }
            return emailConfirmationCode.FirstOrDefault();
        }

        private async Task DeleteEmailConfirmationCodeAsync(string codeHash)
        {
            await _emailConfirmationCodeContainer.DeleteItemAsync<EmailConfirmationCode>(codeHash, new PartitionKey(codeHash));
        }
    }
}
