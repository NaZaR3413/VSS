using Microsoft.AspNetCore.Components;

namespace web_backend.Blazor.Client
{
    public class PageLayoutBase : LayoutComponentBase
    {
        [Parameter]
        public string Sport { get; set; } = "default";
    }
}
