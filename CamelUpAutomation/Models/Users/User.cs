using Microsoft.AspNetCore.Identity;
using System;

namespace CamelUpAutomation.Models.Users
{
    public class User : IdentityUser
    {
        EmailConfirmationCodeAction EmailConfirmationCodeAction { get; set; }
        public string userId { get; set; }

        public string id { get; set; }

    }

   
}
