using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models
{
    public class BettingTickets
    {
        int Id { get; set; }

        int CamelId { get; set; }
        int WinAmount { get; set; }

        Camel? Camel { get; set; }
    }
}
