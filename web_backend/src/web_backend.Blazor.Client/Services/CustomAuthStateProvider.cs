using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Try to get user data from local storage
                var userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_user");
                
                if (string.IsNullOrEmpty(userJson))
                {
                    Console.WriteLine("No user found in local storage");
                    return new AuthenticationState(_anonymous);
                }

                // Deserialize user data
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson);
                
                if (userInfo == null)
                {
                    Console.WriteLine("Failed to deserialize user from storage");
                    return new AuthenticationState(_anonymous);
                }

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userInfo.UserName ?? "User"),
                    new Claim(ClaimTypes.Email, userInfo.Email ?? ""),
                    new Claim("sub", userInfo.Id?.ToString() ?? Guid.NewGuid().ToString())
                };

                // Add roles if available
                if (userInfo.Roles != null)
                {
                    foreach (var role in userInfo.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                // Create authenticated user
                var identity = new ClaimsIdentity(claims, "CustomAuth");
                var user = new ClaimsPrincipal(identity);

                Console.WriteLine($"User authenticated: {userInfo.UserName}");
                
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting auth state: {ex.Message}");
                return new AuthenticationState(_anonymous);
            }
        }

        public async Task SetAuthenticatedUser(UserInfo userInfo)
        {
            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfo.UserName ?? "User"),
                new Claim(ClaimTypes.Email, userInfo.Email ?? ""),
                new Claim("sub", userInfo.Id?.ToString() ?? Guid.NewGuid().ToString())
            };

            // Add roles if available
            if (userInfo.Roles != null)
            {
                foreach (var role in userInfo.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // Create authenticated user
            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var user = new ClaimsPrincipal(identity);

            // Store user in local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_user", JsonSerializer.Serialize(userInfo));
            
            // Notify authentication state has changed
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task ClearAuthenticationState()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_user");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }

    public class UserInfo
    {
        public Guid? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public string? Name { get; set; }
    }
}