using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace CamelUpAutomation.Auth
{
    public class Policies
    {
        public static AuthorizationPolicy Owner()
        {
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
            policy.Requirements.Add(new RoleRequirement(AuthRole.Admin));
            return policy.Build();
        }

        public static AuthorizationPolicy Follower()
        {
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
            policy.Requirements.Add(new AnyRoleRequirement(new List<AuthRole> { AuthRole.Admin, AuthRole.Contributor }));
            return policy.Build();
        }

        public static AuthorizationPolicy AnyPolicy()
        {
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
            return policy.Build();
        }
    }
}

