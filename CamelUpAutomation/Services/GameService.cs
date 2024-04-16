﻿using CamelUpAutomation.Enums;
using CamelUpAutomation.Models;
using CamelUpAutomation.Models.Game;
using CamelUpAutomation.Models.Players;
using CamelUpAutomation.Models.ReturnObjects;
using CamelUpAutomation.Repos;
using DurableTask.Core.History;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamelUpAutomation.Repos.GameUserRepo;

namespace CamelUpAutomation.Services
{
    public interface IGameService
	{
		Task<ServiceResult<Game>> CreateGame(string name, string userId, bool isPrivate, string code);
		Task<ServiceResult<IEnumerable<Game>>> GetGames(string userId, int skip, int take);
		Task<ServiceResult<Game>> UpdateGame(Game game, string userId);

		Task<ServiceResult<Game>> GetGame(string gameId);
		Task<ServiceResult> PlaceSpectatorTile(string gameId, string userId, int tilePosition, CheerTileMode mode);
		Task<ServiceResult> CreateRollAction(string gameId, string userId);
		Task<ServiceResult> AddRollNumber(string gameId, int rollNumber, CamelColor color);
		Task<ServiceResult> AddLegBet(string gameId, string userId, string ticketId);
		Task<ServiceResult> AddRaceBet(string gameId, string userId, CamelColor camelColor, bool isWinnerBet);
		Task<ServiceResult> AddPartnership(string gameId, string userId, string partnershipPlayerId);
		Task<ServiceResult<Game>> AddPlayer(string gameId, string userId, string playerName, string code);
		Task<ServiceResult<Game>> RemovePlayer(string gameId, string playerUserId, string userId);
	}

	public class GameService : IGameService
	{
		private readonly ICryptoService _cryptoService;
		private readonly IUserService _userService;
		private readonly IGameRepo _gameRepo;
		private readonly IGameLogicService _gameLogicService;
		private readonly IGameUserRepo _gameUserRepo;

		public GameService(ICryptoService cryptoService, IGameRepo gameRepo, IGameLogicService gameLogicService, IGameUserRepo gameUserRepo)
        {
            this._cryptoService = cryptoService;
            this._gameRepo = gameRepo;
            this._gameLogicService = gameLogicService;
            this._gameUserRepo = gameUserRepo;
        }

         public async Task<ServiceResult<Game>> CreateGame(string name, string userId, bool isPrivate, string code)
		{
			if (isPrivate && string.IsNullOrEmpty(code))
			{
                return ServiceResult<Game>.FailedResult("Code is required for private games");
            }
			// create a game object 
			Game game = new Game
			{
				id = _cryptoService.GenerateRandomString(),
				IsPrivate = isPrivate,
				Code = code,
				Name = name,
				Turn = 0,
				Round = 0, // setting round to 1 starts the game 
				RoundRoles = 0,
				CreatedAt = DateTime.Now,
				CreatedBy = userId,
				Players = null,
				Camels = null,
			};
			GenerateBettingTickets(game);
			AddCamels(game);
			await _gameRepo.AddGame(game);
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public async Task<ServiceResult<Game>> UpdateGame(Game game, string userId)
		{
            Game dbGame = await _gameRepo.GetGame(game.id);
			if (dbGame.CreatedBy != userId)
			{
				return ServiceResult<Game>.FailedResult("Only the creator of the game can edit it");
			}
			dbGame.Code = game.Code;
			dbGame.IsPrivate = game.IsPrivate;
			dbGame.Name = game.Name;
			await _gameRepo.UpdateGame(dbGame);
			return ServiceResult<Game>.SuccessfulResult(dbGame);
        }

		public async Task<ServiceResult<IEnumerable<Game>>> GetGames(string userId, int skip, int take)
		{
			IEnumerable<GameUser> gameUsers = await _gameUserRepo.GetGameUsersByUserId(userId);
			string[] gameIds = gameUsers.Select(gu => gu.GameId).ToArray();
            IEnumerable<Game> games = await _gameRepo.GetGames(userId, gameIds, skip, take);
            if (games == null)
			{
                return ServiceResult<IEnumerable<Game>>.FailedResult("No games found");
            }
            return ServiceResult<IEnumerable<Game>>.SuccessfulResult(games);
        }

		public async Task<ServiceResult> PlaceSpectatorTile(string gameId, string userId, int tilePosition, CheerTileMode mode)
		{
			ServiceResult<Game> validateGameAndPlayerResult = await ValidateGameAndPlayer(gameId, userId);
			if (!validateGameAndPlayerResult.IsSuccessful)
			{
				return validateGameAndPlayerResult;
			}
			var game = validateGameAndPlayerResult.Result;
			var player = game.Players.FirstOrDefault(p => p.UserId == userId);
			var gameResult = _gameLogicService.PlaceSpectatorTile(game, player, tilePosition, mode);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			await _gameRepo.UpdateGame(gameResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> CreateRollAction(string gameId, string userId)
		{
			ServiceResult<Game> validateGameAndPlayerResult = await ValidateGameAndPlayer(gameId, userId);
			if (!validateGameAndPlayerResult.IsSuccessful)
			{
				return validateGameAndPlayerResult;
			}
			var game = validateGameAndPlayerResult.Result;
			var player = game.Players.FirstOrDefault(p => p.UserId == userId);
			var gameResult = _gameLogicService.CreateRollAction(game, player);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			await _gameRepo.UpdateGame(gameResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> AddRollNumber(string gameId, int rollNumber, CamelColor color)
		{
			ServiceResult<Game> gameResult = await GetGame(gameId);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			var game = gameResult.Result;
			var gameLogicResult = _gameLogicService.AddRollNumber(game, rollNumber, color);
			if (!gameLogicResult.IsSuccessful)
			{
                return gameLogicResult;
            }
			game = gameLogicResult.Result;
			if (game.IsGameFinished())
			{
				gameResult = _gameLogicService.EndGame(game);
				if (!gameResult.IsSuccessful)
				{
                    return gameResult;
                }
			}
			if (game.IsRoundFinished())
			{
				gameResult = _gameLogicService.EndLeg(game);
				if (!gameResult.IsSuccessful)
				{
                    return gameResult;
                }
				game.RoundRoles = 0;
			}
			await _gameRepo.UpdateGame(gameLogicResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> AddLegBet(string gameId, string userId, string ticketId)
		{
			ServiceResult<Game> validateGameAndPlayerResult = await ValidateGameAndPlayer(gameId, userId);
			if (!validateGameAndPlayerResult.IsSuccessful)
			{
				return validateGameAndPlayerResult;
			}
			var game = validateGameAndPlayerResult.Result;
			var player = game.Players.FirstOrDefault(p => p.UserId == userId);
			var ticket = game.BettingTickets.FirstOrDefault(t => t.id == ticketId);
			var gameResult = _gameLogicService.AddLegBet(game, player, ticket);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			await _gameRepo.UpdateGame(gameResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> AddRaceBet(string gameId, string userId, CamelColor camelColor, bool isWinnerBet)
		{
			ServiceResult<Game> validateGameAndPlayerResult = await ValidateGameAndPlayer(gameId, userId);
			if (!validateGameAndPlayerResult.IsSuccessful)
			{
				return validateGameAndPlayerResult;
			}
			var game = validateGameAndPlayerResult.Result;
			var player = game.Players.FirstOrDefault(p => p.UserId == userId);
			var gameResult = _gameLogicService.AddRaceBet(game, player, camelColor, isWinnerBet);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			await _gameRepo.UpdateGame(gameResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult> AddPartnership(string gameId, string userId, string partnershipPlayerId)
		{
			ServiceResult<Game> validateGameAndPlayerResult = await ValidateGameAndPlayer(gameId, userId);
			if (!validateGameAndPlayerResult.IsSuccessful)
			{
				return validateGameAndPlayerResult;
			}
			var game = validateGameAndPlayerResult.Result;
			if (game.Players.Length < 6)
			{
				return ServiceResult.FailedResult("Not enough players to add partnership");
			}
			var playerOne = game.Players.FirstOrDefault(p => p.UserId == userId);
			var playerTwo = game.Players.FirstOrDefault(p => p.id == partnershipPlayerId);
			var gameResult = _gameLogicService.AddPartnership(game, playerOne, playerTwo);
			if (!gameResult.IsSuccessful)
			{
                return gameResult;
            }
			await _gameRepo.UpdateGame(gameResult.Result);
			// send to signal R
			return ServiceResult.SuccessfulResult();
		}

		public async Task<ServiceResult<Game>> AddPlayer(string gameId, string userId, string playerName, string code)
		{
			ServiceResult<Game> gameResult = await GetGame(gameId);
			if (!gameResult.IsSuccessful)
			{
				return gameResult;
			}
			var game = gameResult.Result;
			if (game.IsPrivate && game.Code != code)
			{
                return ServiceResult<Game>.FailedResult("Invalid code", ServiceResponseCode.Forbidden);
            }

			ServiceResult<Models.Users.User> userResult = await _userService.GetUser(userId);
			if (!userResult.IsSuccessful)
			{
                return ServiceResult<Game>.FailedResult("User not found");
            }

		
			var user = userResult.Result;
			AddPlayerToGame(game, user, playerName);
			await _gameRepo.UpdateGame(game);
			await _gameUserRepo.AddGameUser(new GameUser
			{
                id = _cryptoService.GenerateRandomString(),
                GameId = game.id,
                UserId = user.id
            });
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public async Task<ServiceResult<Game>> RemovePlayer(string gameId, string playerUserId, string userId)
        {
            ServiceResult<Game> gameResult = await GetGame(gameId);
            if (!gameResult.IsSuccessful)
            {
                return gameResult;
            }
            ServiceResult<Models.Users.User> userResult = await _userService.GetUser(userId);
            if (!userResult.IsSuccessful)
            {
                return ServiceResult<Game>.FailedResult("User not found", ServiceResponseCode.NotFound);
            }

            var game = gameResult.Result;

			if (game.Turn > 0)
			{
				return ServiceResult<Game>.FailedResult("Game has already started", ServiceResponseCode.Forbidden);
			}
            if (game.Players.FirstOrDefault(p => p.UserId == userId) == null)
            {
                return ServiceResult<Game>.FailedResult("Player not found in game", ServiceResponseCode.NotFound);
            }
			if (game.CreatedBy != userId && playerUserId != userId)
			{
				return ServiceResult<Game>.FailedResult("Only the creator of the game can remove players", ServiceResponseCode.Forbidden);
			}
            game.Players = game.Players.Where(p => p.UserId != userId).ToArray();
			await _gameUserRepo.DeleteGameUser(userId);
			await _gameRepo.UpdateGame(game);
			return ServiceResult<Game>.SuccessfulResult(game);
        }

        private void AddPlayerToGame(Game game, Models.Users.User User, string playerName)
		{
			IList<Player> playerList;
			if (game.Players == null)
			{
                playerList = new List<Player>();
            }
            else
			{
                playerList = game.Players.ToList();
            }
		
			Player player = new Player
			{
				id = _cryptoService.GenerateRandomString(),
				UserId = User.id,
				GameId = game.id,
				Name = playerName,
				PartnerId = null,
				PlayPosition = playerList.Count,
				Balance = 3
			};
			playerList.Add(player);
			game.Players = playerList.ToArray();
		}

		// validators 

		private async Task<ServiceResult<Game>> ValidateGameAndPlayer(string gameId, string userId)
		{
			ServiceResult<Game> gameResult = await GetGame(gameId);
			if (!gameResult.IsSuccessful)
			{
				return gameResult;
			}
			var game = gameResult.Result;
			if (game.Players.FirstOrDefault(p => p.UserId == userId ) == null)
			{
                return ServiceResult<Game>.FailedResult("Player not found in game", ServiceResponseCode.NotFound);
            }
			if (game.Round < 1)
			{
				return ServiceResult<Game>.FailedResult("Game has not started yet", ServiceResponseCode.Forbidden);
			}
			return gameResult;
		}


		// generate a betting ticket for each color of the enum CamelColor 
		private void GenerateBettingTickets(Game game)
		{
            IList<BettingTicket> bettingTickets = new List<BettingTicket>();
            foreach (CamelColor color in Enum.GetValues(typeof(CamelColor)))
			{
				int[] winAmmounts = { 2, 2, 3, 5 };
				foreach (int winAmount in winAmmounts)
				{
					BettingTicket bettingTicket = new BettingTicket
					{
						id = _cryptoService.GenerateRandomString(),
						CamelColor = color,
						WinAmount = winAmount
					};
					bettingTickets.Add(bettingTicket);
				}
			}
            game.BettingTickets = bettingTickets.ToArray();
        }

		private void AddCamels(Game game)
		{
            IList<Camel> camels = new List<Camel>();
            foreach (CamelColor color in Enum.GetValues(typeof(CamelColor)))
			{
                Camel camel = new Camel
				{
                    id = _cryptoService.GenerateRandomString(),
                    Color = color,
                    Position = 0,
                    Height = 0,
                    IsCrazyCamel = (color == CamelColor.White || color == CamelColor.Black)
                };
                camels.Add(camel);
            }
            game.Camels = camels.ToArray();
        }

		public async Task<ServiceResult<Game>> GetGame(string gameId) {
            Game game = await _gameRepo.GetGame(gameId);
            if (game == null)
            {
                return ServiceResult<Game>.FailedResult("Game not found");
            }
            return ServiceResult<Game>.SuccessfulResult(game);
        }

	}
}
