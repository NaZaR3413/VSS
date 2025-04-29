using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace web_backend.Controllers
{
    [Route("hls")]
    [ApiController]
    public class StreamProxyController : AbpController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _streamServerBaseUrl = "http://20.3.254.14:8080";

        public StreamProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{**path}")]
        public async Task<IActionResult> ProxyHlsStream(string path)
        {
            try
            {
                // Create an HttpClient to forward the request
                var httpClient = _httpClientFactory.CreateClient("StreamProxy");

                // Build the full URL for the HLS stream
                var fullUrl = $"{_streamServerBaseUrl}/hls/{path}";

                // Log the request for debugging
                Logger.LogInformation($"Proxying HLS request to: {fullUrl}");

                // Forward the request to the stream server
                var response = await httpClient.GetAsync(fullUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning($"Failed to proxy stream: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode);
                }

                // Get content and headers
                var content = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/x-mpegURL";

                // Return the stream content with appropriate headers
                return File(content, contentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error proxying HLS stream");
                return StatusCode(500, new { error = "Failed to proxy stream" });
            }
        }
    }
}
