using CamelUpAutomation.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models.Players
{
    public class Player
    {
        public string id { get; set; }

        public string UserId { get; set; }

        public string GameId { get; set; }

        public string Name { get; set; }

        public string? PartnerId { get; set; }
        public int Balance { get; set; }

        public int PlayPosition { get; set; }
    }
}
