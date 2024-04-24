using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class AddPartnershipDto
    {
        [Required(ErrorMessage = "GameId is required")]
        public string GameId { get; set; }

        [Required(ErrorMessage = "PartnershipPlayerId is required")]
        public string PartnershipPlayerId { get; set; }
    }
}
