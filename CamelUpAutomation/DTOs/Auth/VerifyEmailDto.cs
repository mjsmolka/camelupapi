using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Auth
{
    public class VerifyEmailDto
    {
        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; }
    }
}
