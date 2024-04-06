using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models.Users
{
    public class EmailConfirmationCode
    {
        public string id { get; set; }
        public string UserId { get; set;}
        public DateTime ExperationTime { get; set; }
        public string CodeHash { get; set; }

        public EmailConfirmationCodeAction Action { get; set; }
    }

    public enum EmailConfirmationCodeAction
    {
        ConfirmEmail,
        ResetPassword
    }
}
