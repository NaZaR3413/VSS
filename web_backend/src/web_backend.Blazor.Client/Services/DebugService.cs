using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Volo.Abp.DependencyInjection;

namespace web_backend.Blazor.Client.Services
{
    public class DebugService : ISingletonDependency
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<DebugService> _logger;
        private readonly List<string> _logs = new List<string>();
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private bool _isInitialized = false;

        public DebugService(IJSRuntime jsRuntime, ILogger<DebugService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task Initialize()
        {
            if (_isInitialized) return;
            
            await LogAsync("DebugService initialized");
            _isInitialized = true;
        }

        public async Task LogAsync(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            
            _logs.Add(logMessage);
            _logBuilder.AppendLine(logMessage);
            
            _logger.LogInformation(message);
            
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", logMessage);
            }
            catch (Exception)
            {
                // Ignore JS runtime issues
            }
        }

        public string GetAllLogs()
        {
            return _logBuilder.ToString();
        }
        
        public List<string> GetLogsList()
        {
            return _logs;
        }
    }
}