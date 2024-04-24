using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class StartGameDto
    {
        [Required(ErrorMessage = "GameId is required")]
        public string GameId { get; set; }
    }
}
