using CamelUpAutomation.Models.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class AddSpectatorTileDto
    {
        [Required(ErrorMessage = "GameId is required")]
        public string GameId { get; set; }

        [Required(ErrorMessage = "TilePosition is required")]
        [Range(1, 16, ErrorMessage = "TilePosition must be between 1 and 16")]
        public int TilePosition { get; set; }

        [Required(ErrorMessage = "CheerTileMode is required")]
        [EnumDataType(typeof(CheerTileMode), ErrorMessage = "CheerTileMode must be a valid CheerTileMode enum value")]
        public CheerTileMode CheerTileMode { get; set; }
    }
}
