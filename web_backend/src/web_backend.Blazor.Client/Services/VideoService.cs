using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace web_backend.Blazor.Client.Services
{
    public class VideoService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<VideoService> _logger;

        public VideoService(IJSRuntime jsRuntime, ILogger<VideoService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a video player with HLS support
        /// </summary>
        /// <param name="playerElementId">The ID of the HTML element for the player</param>
        /// <param name="hlsUrl">The URL to the HLS stream</param>
        /// <returns>A reference to the player that can be disposed later</returns>
        public async Task<IJSObjectReference> InitializePlayerAsync(string playerElementId, string hlsUrl)
        {
            try
            {
                _logger.LogInformation($"Initializing video player for element {playerElementId} with URL {hlsUrl}");
                return await _jsRuntime.InvokeAsync<IJSObjectReference>("initializeVideoPlayer", playerElementId, hlsUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing video player");
                throw;
            }
        }

        /// <summary>
        /// Disposes a video player
        /// </summary>
        /// <param name="playerReference">The reference returned from InitializePlayerAsync</param>
        public async Task DisposePlayerAsync(IJSObjectReference playerReference)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("disposeVideoPlayer", playerReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing video player");
            }
        }
    }
}