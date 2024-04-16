using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
    public class CreateGameDto
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "IsPrivate is required")]
        public bool IsPrivate { get; set; }
        [Required(ErrorMessage ="Code is required")]
        public string Code { get; set; }
    }
}
