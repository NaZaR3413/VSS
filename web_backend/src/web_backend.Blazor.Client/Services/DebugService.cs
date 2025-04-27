using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    public class DebugService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly List<string> _logs = new List<string>();
        private bool _isInitialized = false;

        public DebugService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task Initialize()
        {
            if (!_isInitialized)
            {
                _logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Debug service initialized");
                try
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", "Debug service initialized");
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Error initializing debug service: {ex.Message}");
                }
            }
        }

        public async Task LogAsync(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";
            
            _logs.Add(logEntry);
            
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", logEntry);
            }
            catch
            {
                // If JS runtime fails, we can't do much but we already stored the log in memory
            }
            
            // Keep log size reasonable
            if (_logs.Count > 1000)
            {
                _logs.RemoveRange(0, 100);
            }
        }

        public string GetAllLogs()
        {
            return string.Join("\n", _logs);
        }
    }
}