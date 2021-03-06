using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Rookie.AMO.Identity.DataAccessor.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Rookie.AMO.Identity.Business.Services
{
    public class CustomProfileService : IProfileService
    {
        private readonly ILogger<CustomProfileService> _logger;
        private readonly UserManager<User> _userManager;
        public CustomProfileService(UserManager<User> userManager,
            ILogger<CustomProfileService> logger)
        {
            _logger = logger;
            _userManager = userManager;
        }
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
            if (sub == null)
            {
                throw new Exception("No sub claim present");
            }

            var user = await _userManager.FindByIdAsync(sub);
            if (user == null)
            {
                _logger.LogWarning("No user found matching subject Id: {0}", sub);
            }
            else
            {
                var userClaims = await _userManager.GetClaimsAsync(user);
                var customClaims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.Subject, user.Id.ToString(CultureInfo.InvariantCulture)),
                    new Claim("userName", user.UserName),
                    new Claim("changePasswordTimes", user.ChangePasswordTimes.ToString())
                };

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var userRole in userRoles)
                {
                    customClaims.Add(new Claim(JwtClaimTypes.Role, userRole));
                }

                context.IssuedClaims.AddRange(userClaims);
                context.IssuedClaims.AddRange(customClaims);
            }
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.FindFirst(IdentityModel.JwtClaimTypes.Subject)?.Value;
            if (sub == null)
            {
                throw new Exception("No subject Id claim present");
            }

            var user = await _userManager.FindByIdAsync(sub);
            if (user == null)
            {
                _logger.LogWarning("No user found matching subject Id: {0}", sub);
            }

            context.IsActive = user != null;

        }
    }
}
