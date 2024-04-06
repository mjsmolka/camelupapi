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
        [StringLength(20, ErrorMessage = "Code must be 20 characters long")]
        public string Code { get; set; }
    }
}
