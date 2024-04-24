using CamelUpAutomation.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class AddRollNumberDto
    {
        [Required(ErrorMessage = "GameId is required")]
        public string GameId { get; set; }

        [Required(ErrorMessage = "RollNumber is required")]
        [Range(1, 3, ErrorMessage = "RollNumber must be between 1 and 3")]
        public int RollNumber { get; set; }

        [Required(ErrorMessage = "CamelColor is required")]
        [EnumDataType(typeof(CamelColor), ErrorMessage = "CamelColor must be a valid CamelColor enum value")]
        public CamelColor CamelColor { get; set; }


    }
}
