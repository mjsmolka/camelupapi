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
    public interface IUserRepo
    {
        Task AddUser(Models.Users.User user);

        Task<Models.Users.User> GetUser(string id);
        Task<Models.Users.User> GetUserEmail(string email);
        Task UpdateUser(Models.Users.User user);
        Task DeleteUser(string email);
    }

    public class UserRepo : IUserRepo
    {
        private readonly Container _userContainer;
        public UserRepo(IClientFactory clientFactory) {
            _userContainer = clientFactory.GetContainer(ContainerNames.Users);
        }

        public async Task AddUser(Models.Users.User user)
        {
            await _userContainer.CreateItemAsync(user, new PartitionKey(user.userId));
        }

        public async Task<Models.Users.User> GetUserEmail(string email)
        {
            var iterator =  _userContainer.GetItemLinqQueryable<Models.Users.User>()
                                .Where(u => u.Email == email).ToFeedIterator();
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        public async Task<Models.Users.User> GetUser(string id)
        {
            var iterator =  _userContainer.GetItemLinqQueryable<Models.Users.User>()
                                .Where(u => u.userId == id).ToFeedIterator();
            var user = await iterator.ReadNextAsync();

            return user.FirstOrDefault();
        }

        public async Task UpdateUser(Models.Users.User user)
        {
            await _userContainer.UpsertItemAsync(user, new PartitionKey(user.Id));
        }

        public async Task DeleteUser(string id)
        {
            await _userContainer.DeleteItemAsync<Models.Users.User>(id, new PartitionKey(id));
        }
    }
}
