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
using CamelUpAutomation.Services;

namespace CSharp
{
    
    public class SignalRFunction
    {
        private readonly IAuthService _authService;
        private readonly IGameService _gameService;
        private readonly ISignalRService _signalRService;

        public SignalRFunction(IAuthService authService,
            IValidatorService validatorService,
            ISignalRService signalRService,
            IGameService gameService, 
            IConfiguration config)
        {
            _authService = authService;
            _gameService = gameService;
            _signalRService = signalRService;
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
            var negotiateResponse = await _signalRService.Negotiate(tokenResult.Result);
            return negotiateResponse.ActionResult;
        }
    }
}