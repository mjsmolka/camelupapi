using CamelUpAutomation.Enums;
using CamelUpAutomation.Models;
using CamelUpAutomation.Models.Game;
using CamelUpAutomation.Models.Players;
using CamelUpAutomation.Models.ReturnObjects;
using Microsoft.Azure.Cosmos.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{
	public interface IGameLogicService
	{
		public ServiceResult<Game> PlaceSpectatorTile(Game game, Player player, int tilePosition, CheerTileMode mode);
		public ServiceResult<Game> CreateRollAction(Game game, Player player);
		public ServiceResult<Game> AddRollNumber(Game game, int rollNumber, CamelColor color);
		public ServiceResult<Game> AddLegBet(Game game, Player player, BettingTicket ticket);

		public ServiceResult<Game> EndGame(Game game);
		public ServiceResult<Game> EndLeg(Game game);
		public ServiceResult<Game> AddRaceBet(Game game, Player player, CamelColor camelColor, bool isWinnerBet);
		public ServiceResult<Game> AddPartnership(Game game, Player playerOne, Player playerTwo);
	}
	public class GameLogicService : IGameLogicService
	{
		private readonly ICryptoService _cryptoService;
		private readonly IPayoutLogicService _payoutLogicService;
		public GameLogicService(ICryptoService cryptoService, IPayoutLogicService payoutLogicService)
		{
			_payoutLogicService = payoutLogicService;
			_cryptoService = cryptoService;
		}

		public ServiceResult<Game> PlaceSpectatorTile(Game game, Player player, int tilePosition, CheerTileMode mode)
		{
			var validateSpectatorTilePosition = IsSpectatorTilePlacementValid(game, tilePosition);
			if (!validateSpectatorTilePosition.IsSuccessful)
			{
				return validateSpectatorTilePosition;
			}
	
			var gameAction = GenerateGameAction(game);
			gameAction.PlayerAction = PlayerAction.PlaceSpectatorTile;
			gameAction.SpectatorTilePlacement = new SpectatorTilePlacement
			{
				id = _cryptoService.GenerateRandomString(),
				PlayerId = player.id,
				Position = tilePosition,
				Mode = mode,
			};

			AddGameActionToGame(game, gameAction);
			game.Turn += 1;
			return ServiceResult<Game>.SuccessfulResult(game);	
		}

		public ServiceResult<Game> CreateRollAction(Game game, Player player)
		{
			var gameAction = GenerateGameAction(game);
			gameAction.PlayerAction = PlayerAction.RollDice;
			gameAction.DiceRoll = new DiceRoll
			{
				id = _cryptoService.GenerateRandomString(),
				PlayerId = player.id,
				RollNumber = 0,
				CamelColor = CamelColor.Blue
			};
			AddGameActionToGame(game, gameAction);
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public ServiceResult<Game> AddRollNumber(Game game, int rollNumber, CamelColor color)
		{
			var lastAction = game.Actions.Last();
			if (lastAction.PlayerAction != PlayerAction.RollDice)
			{
				return ServiceResult<Game>.FailedResult("Cannot add roll number to game", ServiceResponseCode.Forbidden);
			}
			lastAction.DiceRoll.RollNumber = rollNumber;
			lastAction.DiceRoll.CamelColor = color;
			game.RoundRoles += 1;
			game.Turn += 1;
			MoveCamels(game);
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public ServiceResult<Game> AddLegBet(Game game, Player player, BettingTicket ticket)
		{
			var validateLegBetResult = ValidateLegBettingTicket(game, ticket.id);
			if (!validateLegBetResult.IsSuccessful)
			{
                return validateLegBetResult;
            }
			var gameAction = game.Actions.First();
			gameAction.PlayerAction = PlayerAction.PlaceLegTicketBet;
			gameAction.LegBet = new LegBet
			{
                id = _cryptoService.GenerateRandomString(),
                BettingTicketId = ticket.id,
                PlayerId = player.id
            };
			AddGameActionToGame(game, gameAction);
			game.Turn += 1;
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public ServiceResult<Game> AddRaceBet(Game game, Player player, CamelColor camelColor, bool isWinnerBet)
		{
			var validateRaceBetResult = ValidateAddRaceBet(game, player, camelColor);
			if (!validateRaceBetResult.IsSuccessful)
			{
				return validateRaceBetResult;
			}

            var gameAction = GenerateGameAction(game);
            gameAction.PlayerAction = PlayerAction.PlaceRaceBet;
            gameAction.RaceBet = new RaceBet
			{
                id = _cryptoService.GenerateRandomString(),
                PlayerId = player.id,
                CamelColor = camelColor,
                IsWinnerBet = isWinnerBet
            };
            AddGameActionToGame(game, gameAction);
			game.Turn += 1;
            return ServiceResult<Game>.SuccessfulResult(game);
        }

		public ServiceResult<Game> AddPartnership(Game game, Player playerOne, Player playerTwo)
		{
			var validatePartnershipResult = ValidatePartnership(game, playerOne, playerTwo);
			if (!validatePartnershipResult.IsSuccessful)
			{
				return validatePartnershipResult;
			}

            var gameAction = GenerateGameAction(game);
            gameAction.PlayerAction = PlayerAction.EnterPartnership;
			gameAction.Partnership = new Partnership
			{
                id = _cryptoService.GenerateRandomString(),
                PartnerOneId = playerOne.id,
                PartnerTwoId = playerTwo.id
            };
            AddGameActionToGame(game, gameAction);
			game.Turn += 1;
            return ServiceResult<Game>.SuccessfulResult(game);
        }
		
		public ServiceResult<Game> EndLeg(Game game)
		{
			if (!game.IsRoundFinished())
			{
				return ServiceResult<Game>.FailedResult("Cannot end leg", ServiceResponseCode.Forbidden);
			}
			
			_payoutLogicService.AddLegPayouts(game);
			_payoutLogicService.AddPartnershipPayouts(game);
			game.RoundRoles = 0;
			game.Round += 1;
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		public ServiceResult<Game> EndGame(Game game)
		{
			var endLegResult = EndLeg(game);
			if (!endLegResult.IsSuccessful)
			{
                return endLegResult;
            }
			if (!game.IsGameFinished())
			{
				return ServiceResult<Game>.FailedResult("Cannot end game", ServiceResponseCode.Forbidden);
			}
			_payoutLogicService.AddRaceBetPayouts(game);
			return ServiceResult<Game>.SuccessfulResult(game);

		}

		private void MoveCamels(Game game)
		{
			var lastAction = game.Actions.Last();
			
			var camelsToMove = GetRoleCamelStack(game);
			var rollNumber = lastAction.DiceRoll.RollNumber;

			// crazy camels go backwards
			if (camelsToMove.First().IsCrazyCamel) { rollNumber = -rollNumber; };
			
			var newCamelPosition = camelsToMove.First().Position + rollNumber;
			var spectatorTile = GetSpectatorTile(game, newCamelPosition);
			var sendToBottom = false;
			if (spectatorTile != null)
			{
				lastAction.DiceRoll.SpectatorTileId = spectatorTile.id;
				sendToBottom = spectatorTile.Mode == CheerTileMode.Boo;
				var spectatorModifier = spectatorTile.Mode == CheerTileMode.Cheer ? 1 : -1;
				if (camelsToMove.First().IsCrazyCamel) { spectatorModifier = -spectatorModifier; }
				newCamelPosition += spectatorModifier;
				AddSpectatorTilePayout(game, spectatorTile);
			}

			if (rollNumber == 0)
			{
				return;
			}
			var camelStackAtNewPosition = game.Camels.Where(c => c.Position == newCamelPosition).OrderBy(c => c.Height).ToList();
			
			if (sendToBottom)
			{
				StackCamels(game, camelsToMove, camelStackAtNewPosition, newCamelPosition);
			}
			else
			{
				StackCamels(game, camelStackAtNewPosition, camelsToMove, newCamelPosition);
			}
		}

		private void StackCamels(Game game, IEnumerable<Camel> bottomCamels, IEnumerable<Camel> topCamels, int position)
		{
			var height = 0;
			foreach (var camel in bottomCamels)
			{
				camel.Position = camel.Position = position;
				camel.Height = height += 1;
				height += 1;
			}
			foreach (var camel in topCamels)
			{
				camel.Position = camel.Position = position;
				camel.Height = height += 1;
				height += 1;
			}
		}

		private void AddSpectatorTilePayout(Game game, SpectatorTilePlacement spectatorTile)
		{
			var player = game.Players.FirstOrDefault(p => p.id == spectatorTile.PlayerId);
			player.Balance += 1;
		}

		private IEnumerable<Camel> GetRoleCamelStack(Game game)
		{
            var action = game.Actions.Last();
			var color = action.DiceRoll.CamelColor;
			if (color == CamelColor.Black || color == CamelColor.White) { 
				return GetCrazyCamelStack(game, color);
			}
			return GetCamelStack(game, color);
        }
		
		private IEnumerable<Camel> GetCamelStack(Game game, CamelColor color)
		{
			Camel camelToMove = game.Camels.FirstOrDefault(c => c.Color == color);
			return game.Camels.Where(c => c.Position == camelToMove.Position && camelToMove.Height >= camelToMove.Height).OrderBy(c => c.Height);
		}

		// the crazy camel stack carrying another camel is the one that is taken 
		private IEnumerable<Camel> GetCrazyCamelStack(Game game, CamelColor color)
		{
			IEnumerable<Camel> blackStack = GetCamelStack(game, CamelColor.Black);
			IEnumerable<Camel> whiteStack = GetCamelStack(game, CamelColor.White);
			// if crazy camel is dirrectly above another crazy camel, the one on top is taken
			if (blackStack.Count() > 1 && blackStack.ElementAt(1).IsCrazyCamel)
			{
                return whiteStack;
            }
			if (whiteStack.Count() > 1 && whiteStack.ElementAt(1).IsCrazyCamel)
			{
                return blackStack;
            }
			// if a crazy camel has a racing camel on top of it while the other one doesn't, the one with the racing camel is taken
			if (blackStack.Count() > 1 && whiteStack.Count() == 0)
			{
				return blackStack;
			}
			if (whiteStack.Count() > 1 && blackStack.Count() == 0)
			{
				return whiteStack;
			}
			// take the color of the crazy camel that is on the dice roll
			return color == CamelColor.Black ? blackStack : whiteStack;
        }

		private SpectatorTilePlacement GetSpectatorTile(Game game, int position)
		{
			var action = game.Actions.FirstOrDefault(a => 
				a.Round == game.Round &&
				a.PlayerAction == PlayerAction.PlaceSpectatorTile && 
				a.SpectatorTilePlacement.Position == position
			);
			return action != null ? action.SpectatorTilePlacement : null;
		}
		
		
		// Validators 

		private ServiceResult<Game> IsSpectatorTilePlacementValid(Game game, int tilePosition)
		{
			var camelOnLocation = game.Camels.FirstOrDefault(c => c.Position == tilePosition);
			if (camelOnLocation != null)
			{
				return ServiceResult<Game>.FailedResult("Cannot Place a spectator tile where a camel is", ServiceResponseCode.Forbidden);
			}
			var spectatorTiles = game.Actions.FirstOrDefault(a => 
				a.PlayerAction == PlayerAction.PlaceSpectatorTile && 
				a.Round == game.Round &&
				Math.Abs(tilePosition - a.SpectatorTilePlacement.Position) < 2
			);
			if (spectatorTiles != null)
			{
				return ServiceResult<Game>.FailedResult("Cannot Place a spectator tile next to each other", ServiceResponseCode.Forbidden);
			}
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		private void AddGameActionToGame(Game game, GameAction gameAction)
		{
			IList<GameAction> actions;
			if (game.Actions == null)
			{
				actions = new List<GameAction>();
			}
			else
			{
				actions = game.Actions.ToList();
			}
			actions.Add(gameAction);
			game.Actions = actions.ToArray();
		}

		private ServiceResult<Game> ValidateAddRaceBet(Game game, Player player, CamelColor camel)
		{
            var bet = game.Actions.FirstOrDefault(a => 
				a.PlayerAction == PlayerAction.PlaceRaceBet && 
				a.RaceBet.PlayerId == player.id && 
				a.RaceBet.CamelColor == camel
			);
            if (bet != null)
			{
                return ServiceResult<Game>.FailedResult("Already have a bet on this camel", ServiceResponseCode.Forbidden);
			}
			return ServiceResult<Game>.SuccessfulResult(game);
        }

		private ServiceResult<Game> ValidatePartnership(Game game, Player playerOne, Player playerTwo)
		{

            var partnership = game.Actions.FirstOrDefault(a => 
			    a.PlayerAction == PlayerAction.EnterPartnership && 
				a.Round == game.Round &&
				(a.Partnership.PartnerOneId == playerOne.id || a.Partnership.PartnerTwoId == playerOne.id) && 
				(a.Partnership.PartnerOneId == playerTwo.id || a.Partnership.PartnerTwoId == playerTwo.id)
			);
            if (partnership != null)
			{
                return ServiceResult<Game>.FailedResult("Already have a partnership", ServiceResponseCode.Forbidden);
            }
            return ServiceResult<Game>.SuccessfulResult(game);
        }

		private ServiceResult<Game> ValidateLegBettingTicket(Game game, string ticketId)
		{
            var legBets = game.Actions.Where(a => a.Round == game.Round && a.PlayerAction == PlayerAction.PlaceLegTicketBet);
			if (legBets.FirstOrDefault(b => b.LegBet.BettingTicketId == ticketId) != null)
			{
				return ServiceResult<Game>.FailedResult("Already have a bet on this ticket", ServiceResponseCode.Forbidden);
			}
			return ServiceResult<Game>.SuccessfulResult(game);
		}

		// Helpers

		private GameAction GenerateGameAction(Game game)
		{
			return new GameAction
			{
				id = _cryptoService.GenerateRandomString(),
				GameId = game.id,
				Turn = game.Turn,
				Round = game.Round,
			};
		}


		
	}
}
