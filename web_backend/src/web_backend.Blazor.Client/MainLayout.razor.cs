using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client
{
    public partial class MainLayout : LayoutComponentBase
    {
        private bool IsLoggedIn { get; set; }
        private string UserName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            IsLoggedIn = user.Identity?.IsAuthenticated ?? false;

            if (IsLoggedIn)
            {
                UserName = user.Identity.Name;
            }
        }

        [Parameter]
        public string Sport { get; set; } = "default";
    }
}
