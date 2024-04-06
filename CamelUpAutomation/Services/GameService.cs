using CamelUpAutomation.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{
    public interface IGameService
    {
        Task<bool> CreateGame();
        Task<bool> AddPlayerAsync(string playerName);
        Task<bool> AddBettingTicketAsync(string playerName, string camelColor, int winAmount);
        Task<bool> PlaceSpectatorTileAsync(string playerName);
        Task<bool> PlaceLegBetAsync(string playerName, string camelColor);
    }

    public class GameService : IGameService
    {
        private readonly CosmosClient _client;

        public GameService(CosmosClient client)
        {
            this._client = client;
        }

        public async Task<bool> CreateGame()
        {
            // create a game object 
            Game game = new Game
            {
                id = "gamers",
                GameId = 1,
                Code = "Test Game",
                Name = "Camel Up",
                Turn = 1,
                Round = 1,
                RoundRoles = 1,
                Players = null,
                Camels = null,
            };

            await _client.GetDatabase("CamelUp").GetContainer("Games").CreateItemAsync(game);
            return true;
        }

        public Task<bool> AddBettingTicketAsync(string playerName, string camelColor, int winAmount)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddPlayerAsync(string playerName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PlaceLegBetAsync(string playerName, string camelColor)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PlaceSpectatorTileAsync(string playerName)
        {
            throw new NotImplementedException();
        }
    }
}
