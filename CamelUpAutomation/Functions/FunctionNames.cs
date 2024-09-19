using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Functions
{
    public static class AzureFunctionPrefixes
    {
        public const string authPrefix = "auth";
    }

    public static class AzureFunctionNames
    {
        public const string AuthLogin = "AuthLoginAction";
        public const string AuthRegister = "AuthRegisterAction";
        public const string AuthConfirmEmail = "AuthConfirmEmailAction";
        public const string AuthResetPassword = "AuthResetPasswordAction";
        public const string AuthChangePassword = "AuthChangePasswordAction";
        public const string AuthRequestChangePassword = "AuthRequestChangePasswordAction"; // 'Forgot PasswordA
        public const string AuthChangeEmail = "AuthChangeEmailAction";
        public const string AuthLogout = "AuthLogoutAction";
        public const string AuthDeleteAccount = "AuthDeleteAccountAction";

        public const string Ping = "PingAction";

        /* ---------------------- Game --------------------- */ 

        public const string CreateGame = "CreateGameAction";
        public const string JoinGame = "JoinGameAction";
        public const string StartGame = "StartGameAction";
        public const string UpdateGame = "UpdateGameAction";

        public const string RollDice = "RollDiceAction";
        public const string AddRollNumber = "AddRollNumberAction";
        public const string PlaceLegTicket = "PlaceLegTicketAction";
        public const string PlaceRaceBet = "PlaceRaceBetAction";
        public const string PlacePartnership = "EnterPartnershipAction";
        
    }
}
