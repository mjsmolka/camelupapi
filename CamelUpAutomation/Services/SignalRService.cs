using CamelUpAutomation.Models.Game;
using CamelUpAutomation.Models.ReturnObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Services
{
    public interface ISignalRService
    {
        Task SendGameUpdate(Game game);
        Task<ServiceResult<SignalRConnectionInfo>> Negotiate(string userId);
    }

    [SignalRConnection("AzureSignalRConnectionString")]
    public class SignalRService : ServerlessHub, ISignalRService
    {
        public async Task SendGameUpdate(Game game)
        {
             await Clients.Users(game.Players.Select(p => p.UserId).ToList()).SendAsync("gameUpdated", game);
        }

        public async Task<ServiceResult<SignalRConnectionInfo>> Negotiate(string userId)
        {
             var negotiateResponse = await NegotiateAsync(new() { UserId = userId });
            return ServiceResult<SignalRConnectionInfo>.SuccessfulResult(negotiateResponse);
        }
    }
}
