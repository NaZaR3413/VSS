using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using web_backend.Livestreams;
using System;

namespace web_backend.Blazor.Client.Pages
{
    [Authorize]
    public class LiveStreamBase : MainLayout
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Parameter]
        public Guid Id { get; set; }

        protected string VideoUrl { get; set; } = string.Empty;
        protected string StreamTitle { get; set; } = string.Empty;

        protected override void OnParametersSet()
        {
            Console.WriteLine($"Video URL in Blazor: {VideoUrl}");
        }

        protected override async Task OnInitializedAsync()
        {
            if (Id == Guid.Empty)
            {
                StreamTitle = "Invalid live stream ID.";
                return;
            }

            await LoadLiveStreamAsync();
        }
        private async Task LoadLiveStreamAsync()
        {
            try
            {
                HttpClient.BaseAddress = new Uri("https://localhost:44356/");
                var fullUrl = $"{HttpClient.BaseAddress}api/livestreams/{Id}";
                Console.WriteLine($"Fetching livestream from: {fullUrl}");

                var response = await HttpClient.GetAsync($"api/livestreams/{Id}");
                Console.WriteLine($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var liveStream = await response.Content.ReadFromJsonAsync<LivestreamDto>();
                    if (liveStream != null)
                    {
                        Console.WriteLine($"API Data Received: {liveStream.HlsUrl}");
                        VideoUrl = liveStream.HlsUrl;
                        StreamTitle = $"{liveStream.HomeTeam} vs {liveStream.AwayTeam}";
                    }
                    else
                    {
                        Console.WriteLine("API returned null.");
                        StreamTitle = "Live stream not found.";
                    }
                }
                else
                {
                    Console.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    StreamTitle = "Error loading live stream.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                StreamTitle = "Error loading live stream.";
            }
        }

    }
}
