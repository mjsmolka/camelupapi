using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models
{
    public class Game
    {

        public string id { get; set; }
        public int GameId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Turn { get; set; } 
        public int Round { get; set; }
        public int RoundRoles { get; set; }

        public Player[] Players { get; set; }

        public Camel[] Camels { get; set; }
    }
}
