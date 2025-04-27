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
        protected AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        
        // Add back the DebugService but with a different property name
        [Inject] 
        protected DebugService DebugServiceInstance { get; set; } = default!;
        
        private AuthenticationState? previousAuthState;
        
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
            await DebugServiceInstance.LogAsync($"MainLayout.razor.cs: Initial auth state: {previousAuthState?.User?.Identity?.IsAuthenticated}");
        }
        
        private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            try
            {
                var newAuthState = task.Result;
                var wasAuthenticated = previousAuthState?.User?.Identity?.IsAuthenticated ?? false;
                var isAuthenticated = newAuthState?.User?.Identity?.IsAuthenticated ?? false;
                
                DebugServiceInstance.LogAsync($"Auth state changed: Was={wasAuthenticated}, Now={isAuthenticated}").ConfigureAwait(false);
                
                // Only force refresh if authentication status actually changed
                if (wasAuthenticated != isAuthenticated)
                {
                    previousAuthState = newAuthState;
                    // InvokeAsync is called from the component instance in the razor file
                    DebugServiceInstance.LogAsync("MainLayout state changed due to auth change").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                DebugServiceInstance.LogAsync($"Error in auth state change handler: {ex.Message}").ConfigureAwait(false);
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
