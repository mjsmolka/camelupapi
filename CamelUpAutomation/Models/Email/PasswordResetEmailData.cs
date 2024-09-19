using System;
namespace CamelUpAutomation.Models.Email
{
    public class PasswordResetEmailData : TemplateData
    {
        public string Name { get; set; }
        public string ActionUrl { get; set; }
    }
}

