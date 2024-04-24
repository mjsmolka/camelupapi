using CamelUpAutomation.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class AddRaceBetDto
    {
        [Required(ErrorMessage = "GameId is required")]
        public string GameId { get; set; }

        [Required(ErrorMessage = "TicketId is required")]
        public string TicketId { get; set; }

        // required field for Color and must be a enum value of CamelColor
        [Required(ErrorMessage = "Color is required")]
        [EnumDataType(typeof(CamelColor), ErrorMessage = "Color must be a valid CamelColor enum value")]
        public CamelColor Color { get; set; }

        [Required(ErrorMessage = "Position is required")]
        public bool isWinnerBet { get; set; }
    }
}
