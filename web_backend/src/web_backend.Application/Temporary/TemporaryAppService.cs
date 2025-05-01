using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace web_backend.Temporary
{
    public class TemporaryAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;

        public TemporaryAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// API for the web teams in order to check what role a user has while we figure out the auth issues they are expiriencing.
        /// Temporary bandaid. 
        /// </summary>
        /// <param name="username"></param>
        /// <returns>the username and role of the user</returns>
        /// <exception cref="UserFriendlyException"></exception>
        [AllowAnonymous] // allows people who are not logged in to use 
        public async Task<UserRoleDto> GetUserRolesAsync([FromQuery] string username)
        {
            // Find the user by username (no need to normalize)
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                throw new UserFriendlyException($"User with username '{username}' not found.");
            }

            // Get roles using UserManager
            var roleNames = await _userManager.GetRolesAsync(user);

            var role = roleNames.FirstOrDefault() ?? string.Empty; 

            return new UserRoleDto
            {
                Username = username,
                Role = role
            };
        }
    }
}
