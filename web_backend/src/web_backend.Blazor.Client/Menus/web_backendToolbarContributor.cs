using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Components.Web.Theming.Toolbars;

namespace web_backend.Blazor.Client.Menus
{
    public class web_backendToolbarContributor : IToolbarContributor
    {
        public Task ConfigureToolbarAsync(IToolbarConfigurationContext context)
        {
            if (context.Toolbar.Name == StandardToolbars.Main)
            {
                // You can add toolbar items here if needed
                // Example:
                // context.Toolbar.Items.Add(new ToolbarItem(typeof(YourComponent)));
            }

            return Task.CompletedTask;
        }
    }
}