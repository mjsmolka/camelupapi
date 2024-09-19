using System;
namespace CamelUpAutomation.Models.Email
{
	public class WelcomeEmailData : TemplateData
	{
        public string Name { get; set; }
        public string ActionUrl { get; set; }
    }
}

