using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Components.Web;
using web_backend.Blazor.Client.Services;

namespace web_backend.Blazor.Client
{
    public partial class MainLayout
    {
        [Parameter]
        public string Sport { get; set; } = "default";
        
        [Inject]
        protected AuthenticationStateProvider AuthStateProvider { get; set; }
        
        // Removed NavigationManager injection since it's already in MainLayout.razor
        
        private AuthenticationState previousAuthState;
        
        // Renamed to avoid conflict with the OnInitializedAsync in MainLayout.razor
        protected async Task InitializeAuthStateMonitoring()
        {
            // Subscribe to authentication state changes
            if (AuthStateProvider is TokenAuthenticationStateProvider tokenProvider)
            {
                tokenProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            }
            
            // Store initial auth state for comparison
            previousAuthState = await AuthStateProvider.GetAuthenticationStateAsync();
            await DebugService.LogAsync($"MainLayout.razor.cs: Initial auth state: {previousAuthState?.User?.Identity?.IsAuthenticated}");
        }
        
        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            try
            {
                var newAuthState = await task;
                var wasAuthenticated = previousAuthState?.User?.Identity?.IsAuthenticated ?? false;
                var isAuthenticated = newAuthState?.User?.Identity?.IsAuthenticated ?? false;
                
                await DebugService.LogAsync($"Auth state changed: Was={wasAuthenticated}, Now={isAuthenticated}");
                
                // Only force refresh if authentication status actually changed
                if (wasAuthenticated != isAuthenticated)
                {
                    previousAuthState = newAuthState;
                    await InvokeAsync(StateHasChanged);
                    await DebugService.LogAsync("MainLayout state refreshed due to auth change");
                }
            }
            catch (Exception ex)
            {
                await DebugService.LogAsync($"Error in auth state change handler: {ex.Message}");
            }
        }
        
        // Renamed to avoid conflict with the Dispose method in MainLayout.razor
        protected void CleanupAuthStateMonitoring()
        {
            // Unsubscribe from authentication state changes
            if (AuthStateProvider is TokenAuthenticationStateProvider tokenProvider)
            {
                tokenProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            }
        }
    }
}
