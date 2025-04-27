using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Components.Web;
using web_backend.Blazor.Client.Services;

namespace web_backend.Blazor.Client
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Parameter]
        public string Sport { get; set; } = "default";
        
        [Inject]
        protected AuthenticationStateProvider AuthStateProvider { get; set; }
        
        [Inject]
        protected NavigationManager NavigationManager { get; set; }
        
        private AuthenticationState previousAuthState;
        
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            
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
        
        public void Dispose()
        {
            // Unsubscribe from authentication state changes
            if (AuthStateProvider is TokenAuthenticationStateProvider tokenProvider)
            {
                tokenProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            }
        }
    }
}
