using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CamelUpAutomation;
using CamelUpAutomation.Auth;
using CamelUpAutomation.Models.Game;
using CamelUpAutomation.Models.ReturnObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using Microsoft.Azure.SignalR.Management;
using Microsoft.AspNetCore.SignalR;

namespace CSharp
{
    [SignalRConnection("AzureSignalRConnectionString")]
    public class SignalRFunction : ServerlessHub
    {
        private readonly IAuthService _authService;

        public SignalRFunction(IAuthService authService, IConfiguration config) /*: base(serviceProvider) */
        {
            _authService = authService;
        }

        [FunctionName("CosmosTrigger")]
        public async Task Run([CosmosDBTrigger(
            databaseName: "CamelUp",
            containerName:"Games",
            Connection = "CosmosDBConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true
          )] IReadOnlyList<Game> updatedGames)
        {
            if (updatedGames is not null && updatedGames.Any())
            {
                foreach (var doc in updatedGames)
                {
                   await Clients.Users(doc.Players.Select(p => p.UserId).ToList()).SendAsync("gameUpdated", doc);
                }
            }
        }

        [FunctionName("negotiate")]
        public async Task<IActionResult> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "game")] SignalRConnectionInfo connectionInfo)
        {
            req.Headers.TryGetValue("token", out var token);
            ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token);
            if (!tokenResult.IsSuccessful)
            {
                return tokenResult.ActionResult;
            }
            var negotiateResponse = await NegotiateAsync(new() { UserId = tokenResult.Result });
            return (ServiceResult<SignalRConnectionInfo>.SuccessfulResult(negotiateResponse)).ActionResult;
        }
    }
}