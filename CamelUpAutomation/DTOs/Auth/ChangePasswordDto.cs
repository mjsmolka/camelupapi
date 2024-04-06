using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(20, ErrorMessage = "Code must be 20 characters long")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*?\w)(?=.*?\d)(?=.*?[#?!@$ %^&*-]).+$", ErrorMessage = "Password must have at least one character, one number, and one special character")]
        public string Password { get; set; }
    }
}
