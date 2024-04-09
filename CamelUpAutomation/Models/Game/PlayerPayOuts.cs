using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models.Game
{
    public class PlayerPayOut
    {
        public string id; 
        public string PlayerId;
        public string ActionId;

        public int Round; 
        public int Turn;

        public int Amount;
        public string? BettingTicketId;
        public PayOutType PayOutType;
        
    }

    public class DiceRollPayOut
    {
        public string id;
        public string ActionId;
        public string PlayerId;
        public int Turn;
    }

    public enum PayOutType
    {
        DiceRoll,
        SpectatorTile,
        RollDice,
        LegTicketWin,
        LegTicketLoss,
        PartnershipWin,
        RaceBetWin,
        RaceBetLoss,
    }
}
