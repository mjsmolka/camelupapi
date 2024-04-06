using CamelUpAutomation.Enums;
using CamelUpAutomation.Models.Users;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CamelUpAutomation.Repos
{
    
    public interface IClientFactory
    {
        Container GetContainer(ContainerNames containerName);
        string GetContainerName(ContainerNames containerName);
    }

    public class ClientFactory : IClientFactory
    {
        private const string databaseName = "CamelUp";

        private readonly CosmosClient _client;
        public ClientFactory(CosmosClient client) {
            _client = client;
        }

        public Container GetContainer(ContainerNames containerName)
        {
            string containerNameString = GetContainerName(containerName);
            return _client.GetDatabase(databaseName).GetContainer(containerNameString);
        }

        public string GetContainerName(ContainerNames containerName)
		{
			return Enum.GetName(typeof(ContainerNames), containerName);
		}
    }
}
