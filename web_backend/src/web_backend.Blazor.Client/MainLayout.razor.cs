using Microsoft.AspNetCore.Components;

namespace web_backend.Blazor.Client
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Parameter]
        public string Sport { get; set; } = "default";
    }
}
