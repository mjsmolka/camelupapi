using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CamelUpAutomation.Models.ReturnObjects;
using CamelUpAutomation.Auth;
using CamelUpAutomation.DTOs.Auth;
using CamelUpAutomation.DTOs.Game;
using CamelUpAutomation.Services;
using CamelUpAutomation.Enums;
using System.Collections;
using CamelUpAutomation.Models.Game;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace CamelUpAutomation.Functions
{
    [SignalRConnection("AzureSignalRConnectionString")]
    public class GameFunctions : ServerlessHub
    {
        private readonly IAuthService _authService;
        private readonly IValidatorService _validatorService;
        private readonly IGameService _gameService;

        public GameFunctions(IAuthService authService, IValidatorService validatorService, IGameService gameService)
        {
            _validatorService = validatorService;
            _gameService = gameService;
            _authService = authService;
        }

        [FunctionName("GetGames")]
        public async Task<IActionResult> GetGamesAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{mode}/{skip}/{take}")] HttpRequest req,
                    ILogger log, int mode, int skip, int take)
        {
            try
            {
                req.Headers.TryGetValue("token", out var token);
                ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token);
                if (!tokenResult.IsSuccessful)
                {
                    return tokenResult.ActionResult;
                }
                switch ((GameChartMode)mode)
                {
                    case GameChartMode.Public:
                        return (await _gameService.GetPublicGames(skip, take)).ActionResult;
                    case GameChartMode.Created:
                        return (await _gameService.GetGamesByOwner(tokenResult.Result, skip, take)).ActionResult;
                    case GameChartMode.Active:
                        return (await _gameService.GetPlayerGames(tokenResult.Result, skip, take)).ActionResult;
                }


                return ServiceResult.FailedResult("Invalid mode", ServiceResponseCode.BadRequest).ActionResult;
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        [FunctionName("GetGame")]
        public async Task<IActionResult> GetGameDataAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{gameId}")] HttpRequest req,
                    ILogger log, string gameId)
        {
            try
            {
                req.Headers.TryGetValue("token", out var token);
                ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token);
                if (!tokenResult.IsSuccessful)
                {
                    return tokenResult.ActionResult;
                }
                var result = await _gameService.GetGame(gameId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        [FunctionName("DeleteGame")]
        public async Task<IActionResult> DeleteGameAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "game/{gameId}")] HttpRequest req,
                    ILogger log, string gameId)
        {
            try
            {
                req.Headers.TryGetValue("token", out var token);
                ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token);
                if (!tokenResult.IsSuccessful)
                {
                    return tokenResult.ActionResult;
                }
                var result = await _gameService.DeleteGame(gameId, tokenResult.Result);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }


        [FunctionName("GameAction")]
        public async Task<IActionResult> GameAction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game")] HttpRequest req,
            ILogger log)
        {
            try
            {
                req.Headers.TryGetValue("token", out var token);
                ServiceResult<string> tokenResult = _authService.VerifyJWTToken(token);
                if (!tokenResult.IsSuccessful)
                {
                    return tokenResult.ActionResult;
                }
                string routeSlug = req.Query["route"];
                switch (routeSlug)
                {
                    case "create":
                        return await CreateGame(tokenResult.Result, req);
                    case "addLegBet":
                        return await AddLegBet(tokenResult.Result, req);
                    case "addRaceBet":
                        return await AddRaceBet(tokenResult.Result, req);
                    case "addPartnership":
                        return await AddPartnership(tokenResult.Result, req);
                    case "addSpectatorTile":
                        return await AddSpectatorTile(tokenResult.Result, req);
                    case "addRollNumber":
                        return await AddRollNumber(tokenResult.Result, req);
                    case "addRollAction":
                        return await AddRollAction(tokenResult.Result, req);
                    case "addAutoRollAction":
                        return await AddAutoRollAction(tokenResult.Result, req);
                    case "startGame": 
                        return await StartGame(tokenResult.Result, req);
                    case "joinGame":
                        return await JoinGame(tokenResult.Result, req);

                    // Place Leg Ticket
                    default:
                        return ServiceResult.FailedResult("Route not found", ServiceResponseCode.NotFound).ActionResult;
                }


            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> CreateGame(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<CreateGameDto> validateResult = await _validatorService.ValidateDto<CreateGameDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                CreateGameDto dto = validateResult.Result;

                var result = await _gameService.CreateGame(dto.Name, userId, dto.AutoRoll, dto.IsPrivate, dto.Code);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddLegBet(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<AddLegBetDto> validateResult = await _validatorService.ValidateDto<AddLegBetDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.AddLegBet(dto.GameId, userId, dto.TicketId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddRaceBet(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<AddRaceBetDto> validateResult = await _validatorService.ValidateDto<AddRaceBetDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.AddRaceBet(dto.GameId, userId, dto.Color, dto.isWinnerBet);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddPartnership(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<AddPartnershipDto> validateResult = await _validatorService.ValidateDto<AddPartnershipDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.AddPartnership(dto.GameId, userId, dto.PartnershipPlayerId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddSpectatorTile(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<AddSpectatorTileDto> validateResult = await _validatorService.ValidateDto<AddSpectatorTileDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.PlaceSpectatorTile(dto.GameId, userId, dto.TilePosition, dto.CheerTileMode);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddRollNumber(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<AddRollNumberDto> validateResult = await _validatorService.ValidateDto<AddRollNumberDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;
                var gameResult = await _gameService.GetGame(dto.GameId);
                if (gameResult.Result.CreatedBy != userId)
                {
                    return ServiceResult.FailedResult("You are not the owner of this game", ServiceResponseCode.Unauthorized).ActionResult;
                }
                var result = await _gameService.AddRollNumber(dto.GameId, dto.RollNumber, dto.CamelColor);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddRollAction(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<CreateRollActionDto> validateResult = await _validatorService.ValidateDto<CreateRollActionDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.CreateRollAction(dto.GameId, userId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> AddAutoRollAction(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<CreateRollActionDto> validateResult = await _validatorService.ValidateDto<CreateRollActionDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.CreateRollAction(dto.GameId, userId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> StartGame(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<StartGameDto> validateResult = await _validatorService.ValidateDto<StartGameDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.StartGame(dto.GameId, userId);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }

        private async Task<IActionResult> JoinGame(string userId, HttpRequest req)
        {
            try
            {
                ServiceResult<JoinGameDto> validateResult = await _validatorService.ValidateDto<JoinGameDto>(req);
                if (!validateResult.IsSuccessful)
                {
                    return validateResult.ActionResult;
                }
                var dto = validateResult.Result;

                var result = await _gameService.AddPlayer(dto.GameId, userId, dto.Username, dto.Code);
                return result.ActionResult;
            }
            catch (Exception e)
            {
                return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
            }
        }
    }
}
