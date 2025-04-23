using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using web_backend.Enums;
using web_backend.Livestreams;

namespace web_backend.Blazor.Client.Pages
{
    [Authorize]
    public class LiveStreamBase : ComponentBase
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected ILivestreamAppService LivestreamService { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        protected IJSRuntime JS { get; set; }

        [Parameter]
        public Guid Id { get; set; }

        protected string VideoUrl { get; set; } = string.Empty;
        protected string StreamTitle { get; set; } = string.Empty;
        protected AuthenticationState authState;

        protected override async Task OnInitializedAsync()
        {
            if (Id == Guid.Empty)
            {
                StreamTitle = "Invalid live stream ID.";
                return;
            }

            // Get the authentication state for admin role checks
            authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        }
    }
}
