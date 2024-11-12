using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace web_backend.Blazor.Client
{
    public partial class MainLayout : LayoutComponentBase
    {
        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected bool IsLoggedIn { get; set; }
        protected string UserName { get; set; }

        protected override async Task OnInitializedAsync()
        {

        }
    }
}
