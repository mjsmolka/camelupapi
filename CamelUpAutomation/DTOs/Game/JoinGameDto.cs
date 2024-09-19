using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.DTOs.Game
{
	public class JoinGameDto
	{
		[Required(ErrorMessage = "GameId is required")]
		public string GameId { get; set; }
		[Required(ErrorMessage = "UserId is required")]
		public string UserId { get; set; }
		[Required(ErrorMessage = "Username is required")]
		public string Username { get; set; }
		public string Code { get; set; }
	}
}
