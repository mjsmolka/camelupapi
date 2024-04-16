using CamelUpAutomation.Enums;
using CamelUpAutomation.Models.Game;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamelUpAutomation.Repos
{
    public interface IGameRepo
    {
        Task AddGame(Game game);
        Task<IEnumerable<Game>> GetGames(string userId, string[] gameIds, int skip, int take);
        Task<Game> GetGame(string gameId);
        Task UpdateGame(Game game);
        Task DeleteGame(string email);
    }

    public class GameRepo : IGameRepo
    {
        private readonly Container _gameContainer;
        public GameRepo(IClientFactory clientFactory) {
            _gameContainer = clientFactory.GetContainer(ContainerNames.Games);
        }

        public async Task AddGame(Game game)
        {
            await _gameContainer.CreateItemAsync(game, new PartitionKey(game.id));
        }

        public async Task<IEnumerable<Game>> GetGames(string userId, string[] gameIds, int skip, int take)
        {
            var gameIdDictionary = new Dictionary<string, bool>();
            foreach (var gameId in gameIds)
            {
                gameIdDictionary.Add(gameId, true);
            }
            var iterator = _gameContainer.GetItemLinqQueryable<Game>()
                                .Where(u => 
                                    u.IsPrivate == false ||
                                    u.CreatedBy == userId
                                 )
                                .OrderByDescending(u => u.CreatedAt)
                                .Skip(skip)
                                .Take(take)
                                .ToFeedIterator();
            var games = await iterator.ReadNextAsync();

            return games;
        }

        public async Task<Game> GetGame(string gameId)
        {
            var iterator = _gameContainer.GetItemLinqQueryable<Game>()
                                .Where(u => u.id == gameId).ToFeedIterator();
            var game = await iterator.ReadNextAsync();

            return game.FirstOrDefault();
        }
        public async Task UpdateGame(Game game)
        {
            await _gameContainer.UpsertItemAsync(game, new PartitionKey(game.id));
        }

        public async Task DeleteGame(string id)
        {
            await _gameContainer.DeleteItemAsync<Game>(id, new PartitionKey(id));
        }
    }
}
