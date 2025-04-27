using System;

namespace web_backend.Blazor.Client.Services
{
    public class LivestreamStateService
    {
        public event EventHandler? OnLivestreamStatusChanged;

        public void NotifyLivestreamStatusChanged()
        {
            OnLivestreamStatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
