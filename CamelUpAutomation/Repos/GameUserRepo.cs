using CamelUpAutomation.Enums;
using CamelUpAutomation.Models.Game;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Repos
{
	
	public interface IGameUserRepo
	{
		Task AddGameUser(GameUser gameUser);

		Task<IEnumerable<GameUser>> GetGameUsersByGameId(string gameId);
		Task<IEnumerable<GameUser>> GetGameUsersByUserId(string userId);
		Task<GameUser> GetGameByGameUserId(string gameUserId);
		Task DeleteGameUser(string gameUserId);
		Task DeleteAllGameUsersByUserId(string userId);
		Task DeleteAllGameUsersByGameId(string gameId);
	}

	public class GameUserRepo : IGameUserRepo
	{
		private readonly Container _gameUserContainer;
		public GameUserRepo(IClientFactory clientFactory) {
			_gameUserContainer = clientFactory.GetContainer(ContainerNames.Games);
		}

		public async Task AddGameUser(GameUser gameUser)
		{
			await _gameUserContainer.CreateItemAsync(gameUser, new PartitionKey(gameUser.id));
		}

		public async Task DeleteAllGameUsersByGameId(string gameId)
		{
			var gameUsers = await GetGameUsersByGameId(gameId);
			foreach (var gameUser in gameUsers)
			{
				await _gameUserContainer.DeleteItemAsync<GameUser>(gameUser.id, new PartitionKey(gameUser.id));
			}
		}

		public async Task DeleteAllGameUsersByUserId(string userId)
		{
			var gameUsers = await GetGameUsersByUserId(userId);
			foreach (var gameUser in gameUsers)
			{
				await DeleteGameUser(gameUser.id);
			}
		}

		public async Task DeleteGameUser(string gameUserId)
		{
			await _gameUserContainer.DeleteItemAsync<GameUser>(gameUserId, new PartitionKey(gameUserId));
		}

		public async Task<GameUser> GetGameByGameUserId(string gameUserId)
		{
			return await _gameUserContainer.ReadItemAsync<GameUser>(gameUserId, new PartitionKey(gameUserId));
		}

		public async Task<IEnumerable<GameUser>> GetGameUsersByGameId(string gameId)
		{
			var iterator = _gameUserContainer.GetItemLinqQueryable<GameUser>()
							.Where(gu => gu.GameId == gameId)
							.ToFeedIterator();
				
			return await iterator.ReadNextAsync();
		}

		public async Task<IEnumerable<GameUser>> GetGameUsersByUserId(string userId)
		{
			var iterator = _gameUserContainer.GetItemLinqQueryable<GameUser>()
							.Where(gu => gu.GameId == userId)
							.ToFeedIterator();
				
			return await iterator.ReadNextAsync();
		}
	}
}

