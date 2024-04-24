using System;
using System.IO;
using CamelUpAutomation;
using CamelUpAutomation.Auth;
using CamelUpAutomation.Repos;
using CamelUpAutomation.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CamelUpAutomation
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            IConfiguration configuration = builder.GetContext().Configuration;
            builder.Services.AddSingleton<CosmosClient>((serviceProvider) =>
            {
                return new CosmosClient(configuration["CosmosDBConnectionString"]);
            });

            builder.Services.AddSingleton<IGameService, GameService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IEmailService, EmailService>();
            builder.Services.AddSingleton<IValidatorService, ValidatorService>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<IEmailConfirmationCodeService, EmailConfirmationCodeService>();
            builder.Services.AddSingleton<IGameLogicService, GameLogicService>();
            builder.Services.AddSingleton<IPayoutLogicService, PayoutLogicService>();

            //Repos
            builder.Services.AddSingleton<IUserRepo, UserRepo>();
            builder.Services.AddSingleton<IClientFactory, ClientFactory>();
            builder.Services.AddSingleton<IUserRepo, UserRepo>();
            builder.Services.AddSingleton<IEmailConfirmationCodeRepo, EmailConfirmationCodeRepo>();
            builder.Services.AddSingleton<IGameRepo, GameRepo>();
            builder.Services.AddSingleton<IGameUserRepo, GameUserRepo>();

            builder.Services.AddSignalR();

        }
    }
}
