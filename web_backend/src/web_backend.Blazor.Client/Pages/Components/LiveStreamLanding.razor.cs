using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client.Pages.Components
{
    public class LiveStreamLandingBase : MainLayout
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        protected List<LiveStreamDto> LiveStreams { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadLiveStreamsAsync();
        }

        protected async Task ApplyFilter(string filterType, string filterValue)
        {
            LiveStreams = await HttpClient.GetFromJsonAsync<List<LiveStreamDto>>($"/api/livestreams?{filterType}={filterValue}");
            StateHasChanged();
        }

        private async Task LoadLiveStreamsAsync()
        {
            LiveStreams = await HttpClient.GetFromJsonAsync<List<LiveStreamDto>>("/api/livestreams");
        }

        protected class LiveStreamDto
        {
            public int Id { get; set; } // Unique identifier for routing to the specific live stream page
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty; // "Live", "Coming Soon", etc.
            public string Url { get; set; } = string.Empty;
        }
    }
}
