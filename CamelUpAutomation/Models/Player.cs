using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models
{
    public class Player
    {
        int Id { get; set; }
        string Name { get; set; }

        int? PartnerId { get; set; }

        bool SpectatorTilePlaced { get; set; }
    }
}
