using System;
using Microsoft.AspNetCore.Authorization;

namespace CamelUpAutomation.Auth
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public RoleRequirement(AuthRole role)
        {
            Role = role;
        }

        public AuthRole Role { get; set; }
    }
}

