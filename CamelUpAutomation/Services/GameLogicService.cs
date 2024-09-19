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
        public DiceRoll GenerateDiceRoll(Game game);
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

            var addActionResult = AddGameActionToGame(game, gameAction);
            if (!addActionResult.IsSuccessful)
            {
                return addActionResult;
            }
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
                CamelColor = CamelColor.Blue,
                IsFinal = false
            };
            var addActionResult = AddGameActionToGame(game, gameAction);
            if (!addActionResult.IsSuccessful)
            {
                return addActionResult;
            }
            return ServiceResult<Game>.SuccessfulResult(game);
        }

        public ServiceResult<Game> AddRollNumber(Game game, int rollNumber, CamelColor color)
        {
            var lastAction = game.Actions.Last();
            if (lastAction.PlayerAction != PlayerAction.RollDice)
            {
                return ServiceResult<Game>.FailedResult("Cannot add roll number to game", ServiceResponseCode.Forbidden);
            }
            if (lastAction.DiceRoll.CamelColor != CamelColor.Blue && lastAction.DiceRoll.RollNumber != 0)
            {
                return ServiceResult<Game>.FailedResult("Cannot add roll number to game", ServiceResponseCode.Forbidden);
            }


            lastAction.DiceRoll.RollNumber = rollNumber;
            lastAction.DiceRoll.CamelColor = color;
            lastAction.DiceRoll.IsFinal = true;
            game.RoundRoles += 1;
            game.Turn += 1;
            MoveCamels(game);
            _payoutLogicService.AddDiceRollPayout(game);
            return ServiceResult<Game>.SuccessfulResult(game);
        }

        public ServiceResult<Game> AddLegBet(Game game, Player player, BettingTicket ticket)
        {
            var validateLegBetResult = ValidateLegBettingTicket(game, ticket.id);
            if (!validateLegBetResult.IsSuccessful)
            {
                return validateLegBetResult;
            }
            var gameAction = GenerateGameAction(game);
            gameAction.PlayerAction = PlayerAction.PlaceLegTicketBet;
            gameAction.LegBet = new LegBet
            {
                id = _cryptoService.GenerateRandomString(),
                BettingTicketId = ticket.id,
                PlayerId = player.id
            };
            var addActionResult = AddGameActionToGame(game, gameAction);
            if (!addActionResult.IsSuccessful)
            {
                return addActionResult;
            }
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
            var addActionResult = AddGameActionToGame(game, gameAction);
            if (!addActionResult.IsSuccessful)
            {
                return addActionResult;
            }
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
            var addActionResult = AddGameActionToGame(game, gameAction);
            if (!addActionResult.IsSuccessful)
            {
                return addActionResult;
            }
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
            game.IsFinished = true;
            _payoutLogicService.AddRaceBetPayouts(game);
            return ServiceResult<Game>.SuccessfulResult(game);
        }

        private void MoveCamels(Game game)
        {
            var lastAction = game.Actions.Last();

            var camelsToMove = GetRoleCamelStack(game);
            var rollNumber = lastAction.DiceRoll.RollNumber;

            // crazy camels go backwards
            if (camelsToMove.First().IsCrazyCamel) { 
                rollNumber = -rollNumber;
            };

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
            var camelStackAtNewPosition = game.Camels.Where(c => c.Position == newCamelPosition && !camelsToMove.Any(x => x.Color == c.Color)).OrderBy(c => c.Height).ToList();

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
                camel.Height = height;
                height += 1;
            }
            foreach (var camel in topCamels)
            {
                camel.Position = camel.Position = position;
                camel.Height = height;
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
            if (color == CamelColor.Black || color == CamelColor.White)
            {
                return GetCrazyCamelStack(game, color);
            }
            return GetCamelStack(game, color);
        }

        private IEnumerable<Camel> GetCamelStack(Game game, CamelColor color)
        {
            Camel camelToMove = game.Camels.FirstOrDefault(c => c.Color == color);
            if (camelToMove.Position == 0 || camelToMove.Position == 17)
            {
                return new List<Camel> { camelToMove };
            }
            return game.Camels.Where(c => c.Position == camelToMove.Position && c.Height >= camelToMove.Height).OrderBy(c => c.Height);
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
            if (blackStack.Count() > 1 && whiteStack.Count() == 1)
            {
                return blackStack;
            }
            if (whiteStack.Count() > 1 && blackStack.Count() == 1)
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

        private ServiceResult<Game> AddGameActionToGame(Game game, GameAction gameAction)
        {
            if (game.IsGameFinished())
            {
                 return ServiceResult<Game>.FailedResult("Cannot Place a spectator tile next to each other", ServiceResponseCode.Forbidden);
            }
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
            return ServiceResult<Game>.SuccessfulResult(game);
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

        private List<CamelColor> GetPossibleCamelColorRoles(Game game, bool ignoreLast = false)
        {
            var camelColorList = Enum.GetValues(typeof(CamelColor)).Cast<CamelColor>().ToList();
            if (game.Actions == null || game.Actions.Length == 0)
            {
                return camelColorList;
            }

            var round = game.Round;
            var roles = game.Actions.Where(a => a.Round == round && a.PlayerAction == PlayerAction.RollDice && a.DiceRoll.IsFinal).ToList();
            // iterate through each role but skip the first one if ignoreLast is true
            if (roles.FirstOrDefault(x => x.DiceRoll.CamelColor == CamelColor.Black || x.DiceRoll.CamelColor == CamelColor.White) != null)
            {
                 camelColorList.Remove(CamelColor.Black);
                 camelColorList.Remove(CamelColor.White);
            }
            roles.ForEach(r => camelColorList.Remove(r.DiceRoll.CamelColor));
            return camelColorList;
        }

        public DiceRoll GenerateDiceRoll(Game game)
        {
            var possibleCamelColors = GetPossibleCamelColorRoles(game);
            if (possibleCamelColors.Contains(CamelColor.Black) && possibleCamelColors.Contains(CamelColor.White))
            {
                possibleCamelColors.Remove(CamelColor.White);
            }
            var random = new Random();
            var camelColor = possibleCamelColors[random.Next(possibleCamelColors.Count)];
            var role = random.Next(1, 4);
            if (camelColor == CamelColor.Black)
            {
                camelColor = random.Next(0, 2) == 0 ? CamelColor.Black : CamelColor.White;
            }
            return new DiceRoll
            {
                CamelColor = camelColor,
                RollNumber = role
            };
        }
    }
}
