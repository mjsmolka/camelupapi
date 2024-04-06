using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace CamelUpAutomation.Auth
{
    public class AnyRoleRequirement : IAuthorizationRequirement
    {
        public AnyRoleRequirement(List<AuthRole> roles)
        {
            Roles = roles;
        }

        public List<AuthRole> Roles { get; set; }
    }
}

