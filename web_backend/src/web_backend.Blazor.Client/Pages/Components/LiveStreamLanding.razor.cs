using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client.Pages
{
    [Authorize]
    public class LiveStreamLandingBase : MainLayout
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        protected List<LiveStreamDto> LiveStreams { get; set; } = new();
        protected string FilterType { get; set; } = string.Empty;
        protected string FilterValue { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var isAuthorized = await CheckUserAuthorizationAsync();
            if (!isAuthorized)
            {
                return;
            }

            await LoadLiveStreamsAsync();
        }

        private async Task<bool> CheckUserAuthorizationAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                NavigationManager.NavigateTo("/Account/Login", true);
                return false;
            }

            var subscriptionClaim = user.FindFirst("Subscription");
            if (subscriptionClaim == null || subscriptionClaim.Value != "Active")
            {
                NavigationManager.NavigateTo("/placeholder.......", true);
                return false;
            }

            return true;
        }

        private async Task LoadLiveStreamsAsync()
        {
            try
            {
                var streams = await HttpClient.GetFromJsonAsync<List<LiveStreamDto>>("/api/livestreams");

                if (streams != null)
                {
                    LiveStreams = streams;
                }
                else
                {
                    LiveStreams = new List<LiveStreamDto>();
                }
            }
            catch
            {
                LiveStreams = new List<LiveStreamDto>();
            }
        }

        protected void ApplyFilter(string filterType, string filterValue)
        {
            FilterType = filterType;
            FilterValue = filterValue;

            LiveStreams = LiveStreams.FindAll(stream =>
                filterType == "sport" ? stream.Sport == filterValue :
                filterType == "team" ? stream.Team == filterValue :
                true);
        }

        protected class LiveStreamDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Sport { get; set; } = string.Empty;
            public string Team { get; set; } = string.Empty;
        }
    }
}
