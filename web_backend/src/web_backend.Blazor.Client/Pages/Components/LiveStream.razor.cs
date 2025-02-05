using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace web_backend.Blazor.Client.Pages
{
    [Authorize]
    public class LiveStreamBase : MainLayout
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Parameter]
        public int Id { get; set; }

        protected string VideoUrl { get; set; } = string.Empty;
        protected string StreamTitle { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var isAuthorized = await CheckUserAuthorizationAsync();
            if (!isAuthorized)
            {
                return;
            }

            await LoadLiveStreamAsync();
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
                NavigationManager.NavigateTo("livestream", true);
                return false;
            }

            return true;
        }

        private async Task LoadLiveStreamAsync()
        {
            try
            {
                var liveStream = await HttpClient.GetFromJsonAsync<LiveStreamDto>($"/api/livestreams/{Id}");

                if (liveStream != null)
                {
                    VideoUrl = liveStream.VideoUrl;
                    StreamTitle = liveStream.Title;
                }
                else
                {
                    StreamTitle = "Live stream not found.";
                }
            }
            catch
            {
                StreamTitle = "Error loading live stream.";
            }
        }

        protected class LiveStreamDto
        {
            public string Title { get; set; } = string.Empty;
            public string VideoUrl { get; set; } = string.Empty;
        }
    }
}
