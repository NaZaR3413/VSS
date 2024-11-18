using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client.Pages
{
    public class LiveStreamBase : MainLayout
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Parameter]
        public int Id { get; set; }

        protected string VideoUrl { get; set; } = string.Empty;
        protected string StreamTitle { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadLiveStreamAsync();
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
