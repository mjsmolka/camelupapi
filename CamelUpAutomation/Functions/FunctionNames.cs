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
        public const string AuthChangeEmail = "AuthChangeEmailAction";
        public const string AuthLogout = "AuthLogoutAction";
    }
}
