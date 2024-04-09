using CamelUpAutomation.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models.Game
{
    public class BettingTicket
    {
        public string id { get; set; }

        public string CamelId { get; set; }
        public int WinAmount { get; set; }

        public CamelColor CamelColor { get; set; }
    }
}
