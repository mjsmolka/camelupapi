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

namespace CamelUpAutomation.Functions
{
	public class GameFunctions
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
            } catch (Exception e)
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
            } catch (Exception e)
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
            } catch (Exception e)
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
                        // Place Leg Ticket
                    default:
						return ServiceResult.FailedResult("Route not found", ServiceResponseCode.NotFound).ActionResult;
				}


			} catch (Exception e)
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

				var result = await _gameService.CreateGame(dto.Name, userId, dto.IsPrivate, dto.Code);
				return result.ActionResult;
			} catch (Exception e)
			{
				return ServiceResult.FailedResult(e.Message, ServiceResponseCode.InternalServerError).ActionResult;
			}
		}
	}
}
