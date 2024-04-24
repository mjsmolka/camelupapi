using CamelUpAutomation.Enums;
using CamelUpAutomation.Models;
using CamelUpAutomation.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{
    public interface IPayoutLogicService
    {
        public void AddLegPayouts(Game game);
        public void AddPartnershipPayouts(Game game);
        public void AddRaceBetPayouts(Game game);
        public void AddDiceRollPayout(Game game);
    }

    public class PayoutLogicService : IPayoutLogicService
    {
        private ICryptoService _cryptoService;
        public PayoutLogicService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        public void AddDiceRollPayout(Game game)
        {
            var lastAction = game.Actions.Last();
            if (lastAction.PlayerAction == PlayerAction.RollDice)
            {
                var payout = new PlayerPayOut
                {
                    id = _cryptoService.GenerateRandomString(),
                    PlayerId = lastAction.DiceRoll.PlayerId,
                    ActionId = lastAction.id,
                    Round = game.Round,
                    Turn = game.Turn,
                    Amount = 1,
                    PayOutType = PayOutType.DiceRoll
                };
                AddPlayerPayout(game, payout);
            }
        }

        public void AddRaceBetPayouts(Game game)
        {
            var camelFinalOrder = game.RaceCamelOrder();
            Camel firstPlaceCamel = camelFinalOrder.First();
            Camel lastPlaceCamel = camelFinalOrder.Last();
            GenerateRaceBetPayouts(game, firstPlaceCamel, true);
            GenerateRaceBetPayouts(game, lastPlaceCamel, false);
        }

        public void AddLegPayouts(Game game)
        {
            var cameOrder = game.RaceCamelOrder();
            var winningCamel = cameOrder.First();
            var secondPlaceCamel = cameOrder.ElementAt(1);

            var legBetActions = game.Actions.Where(a => a.PlayerAction == PlayerAction.PlaceLegTicketBet && a.Round == game.Round);

            var winningBets = legBetActions.Where(b => game.GetBettingTicket(b.LegBet.BettingTicketId).CamelId == winningCamel.id);
            var secondPlaceBets = legBetActions.Where(b => game.GetBettingTicket(b.LegBet.BettingTicketId).CamelId == secondPlaceCamel.id);
            var losingBets = legBetActions.Where(action => !winningBets.Any(x => x.id == action.id) && !secondPlaceBets.Any(x => x.id == action.id));
            foreach (var bet in winningBets)
            {
                var ticket = game.GetBettingTicket(bet.LegBet.BettingTicketId);
                var payout = new PlayerPayOut
                {
                    id = _cryptoService.GenerateRandomString(),
                    PlayerId = bet.LegBet.PlayerId,
                    ActionId = bet.id,
                    Round = game.Round,
                    Turn = game.Turn,
                    Amount = ticket.WinAmount,
                    PayOutType = PayOutType.LegTicketWin
                };
                AddPlayerPayout(game, payout);
            }

            foreach (var bet in secondPlaceBets)
            {
                var ticket = game.GetBettingTicket(bet.LegBet.BettingTicketId);
                var payout = new PlayerPayOut
                {
                    id = _cryptoService.GenerateRandomString(),
                    PlayerId = bet.LegBet.PlayerId,
                    ActionId = bet.id,
                    Round = game.Round,
                    Turn = game.Turn,
                    Amount = 1,
                    PayOutType = PayOutType.LegTicketWin
                };

                AddPlayerPayout(game, payout);
            }

            foreach (var bet in losingBets)
            {
                var ticket = game.GetBettingTicket(bet.LegBet.BettingTicketId);
                var payout = new PlayerPayOut
                {
                    id = _cryptoService.GenerateRandomString(),
                    PlayerId = bet.LegBet.PlayerId,
                    ActionId = bet.id,
                    Round = game.Round,
                    Turn = game.Turn,
                    Amount = -1,
                    PayOutType = PayOutType.LegTicketLoss,
                };
                AddPlayerPayout(game, payout);
            }
        }

        public void AddPartnershipPayouts(Game game)
        {
            var partnershipActions = game.Actions.Where(a => a.PlayerAction == PlayerAction.EnterPartnership && a.Round == game.Round);
            foreach (var action in partnershipActions)
            {
                var playerOne = game.Players.FirstOrDefault(p => p.id == action.Partnership.PartnerOneId);
                var playerTwo = game.Players.FirstOrDefault(p => p.id == action.Partnership.PartnerTwoId);

                var playerOneHighestPayout = game.PlayerPayOuts
                    .Where(p =>
                        p.PlayerId == playerOne.id &&
                        p.Round == game.Round &&
                        p.PayOutType == PayOutType.LegTicketWin &&
                        p.Amount > 0
                    )
                    .OrderByDescending(p => p.Amount)
                    .FirstOrDefault();
                var playerTwoHighestPayout = game.PlayerPayOuts
                    .Where(p =>
                        p.PlayerId == playerOne.id &&
                        p.Round == game.Round &&
                        p.PayOutType == PayOutType.LegTicketWin &&
                        p.Amount > 0
                    )
                    .OrderByDescending(p => p.Amount)
                    .FirstOrDefault();
                if (playerOneHighestPayout != null)
                {
                    var payout = new PlayerPayOut
                    {
                        id = _cryptoService.GenerateRandomString(),
                        PlayerId = playerTwo.id,
                        ActionId = action.id,
                        Round = game.Round,
                        Turn = game.Turn,
                        Amount = playerOneHighestPayout.Amount,
                        BettingTicketId = playerOneHighestPayout.BettingTicketId,
                        PayOutType = PayOutType.PartnershipWin
                    };
                    AddPlayerPayout(game, payout);
                }
                if (playerTwoHighestPayout != null)
                {
                    var payout = new PlayerPayOut
                    {
                        id = _cryptoService.GenerateRandomString(),
                        PlayerId = playerOne.id,
                        ActionId = action.id,
                        Round = game.Round,
                        Turn = game.Turn,
                        Amount = playerOneHighestPayout.Amount,
                        BettingTicketId = playerTwoHighestPayout.BettingTicketId,
                        PayOutType = PayOutType.PartnershipWin
                    };
                    AddPlayerPayout(game, payout);
                }
            }
        }

        private void GenerateRaceBetPayouts(Game game, Camel winningCamel, bool isWinner)
        {
            // generate a stack with 8, 5, 3, 2 with the first element to be popped is 8
            var winAmountsStack = new Stack<int>(new int[] { 8, 5, 3, 2 });
            game.Actions
                .Where(a =>
                    a.PlayerAction == PlayerAction.PlaceRaceBet &&
                    a.RaceBet.IsWinnerBet == isWinner
                )
                .ToList()
                .ForEach(action =>
                {
                    var isWinningBet = action.RaceBet.CamelColor == winningCamel.Color;
                    var payoutType = isWinningBet ? PayOutType.RaceBetWin : PayOutType.RaceBetLoss;
                    var winningAmount = -1;
                    if (isWinningBet)
                    {
                        winningAmount = winAmountsStack.Count > 0 ? winAmountsStack.Pop() : 1;
                    }
                    var payout = new PlayerPayOut
                    {
                        id = _cryptoService.GenerateRandomString(),
                        PlayerId = action.RaceBet.PlayerId,
                        ActionId = action.id,
                        Round = game.Round,
                        Turn = game.Turn,
                        Amount = winningAmount,
                        PayOutType = payoutType
                    };
                    AddPlayerPayout(game, payout);
                });
        }

        private void AddPlayerPayout(Game game, PlayerPayOut payout)
        {
            var payoutLists = game.PlayerPayOuts.ToList();
            payoutLists.Add(payout);
            game.PlayerPayOuts = payoutLists.ToArray();
            game.Players.First(p => p.id == payout.PlayerId).Balance += payout.Amount;
        }
    }
}
