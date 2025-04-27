using System;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client.Services
{
    public class LivestreamStateService
    {
        private readonly NavigationManager _navigationManager;

        public LivestreamStateService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public event EventHandler? OnLivestreamStatusChanged;

        public void NotifyLivestreamStatusChanged()
        {
            // Ensure the event is fired in a thread-safe way
            OnLivestreamStatusChanged?.Invoke(this, EventArgs.Empty);

            // Force a navigation to the same page to trigger a full reload when needed
            // This can help in cases where component state becomes inconsistent
            // _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: false, replace: true);
        }
    }
}
