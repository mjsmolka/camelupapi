using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamelUpAutomation.Models.Players;

namespace CamelUpAutomation.Models.Game
{
    public class Game
    {
        public string id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public int Turn { get; set; }
        public int Round { get; set; }
        public int RoundRoles { get; set; }

        public Player[] Players { get; set; }

        public Camel[] Camels { get; set; }

        public BettingTicket[] BettingTickets { get; set; }

        public GameAction[] Actions { get; set; }

        public PlayerPayOut[] PlayerPayOuts { get; set; }   

        public BettingTicket GetBettingTicket(string ticketId)
        {
            return BettingTickets.FirstOrDefault(t => t.id == ticketId);
        }

        public bool IsRoundFinished()
        {
            return RoundRoles == 5 || IsGameFinished();
        }

        public bool IsGameFinished()
        {
            return RaceCamelOrder().Any(c => c.Position > 16);
        }

        public List<Camel> RaceCamelOrder()
        {
            return Camels
                .Where(c => !c.IsCrazyCamel)
                .OrderByDescending(c => c.Position)
                .ThenByDescending(c => c.Height).ToList();
        }
    }
}
