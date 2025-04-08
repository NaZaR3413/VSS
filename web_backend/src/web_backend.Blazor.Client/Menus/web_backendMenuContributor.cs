using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using web_backend.Localization;
using web_backend.MultiTenancy;
using Volo.Abp.Account.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Blazor;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.UI.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace web_backend.Blazor.Client.Menus;

public class web_backendMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;

    public web_backendMenuContributor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
        else if (context.Menu.Name == StandardMenus.User)
        {
            await ConfigureUserMenuAsync(context);
        }
    }

    private async Task<Task> ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<web_backendResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                web_backendMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fas fa-home"
            )
        );

        var administration = context.Menu.GetAdministration();

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);

        // Add Admin menu item if the user is an admin
        var authState = await context.ServiceProvider.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.IsInRole("Admin"))
        {
            context.Menu.Items.Add(
                new ApplicationMenuItem(
                    "Admin",
                    l["Menu:Admin"],
                    icon: "fas fa-cog"
                ).AddItem(
                    new ApplicationMenuItem(
                        "Admin.Teams",
                        l["Menu:Teams"],
                        "/admin/teams"
                    )
                ).AddItem(
                    new ApplicationMenuItem(
                        "Admin.Scores",
                        l["Menu:Scores"],
                        "/admin/scores"
                    )
                ).AddItem(
                    new ApplicationMenuItem(
                        "Admin.GameTimes",
                        l["Menu:GameTimes"],
                        "/admin/gametimes"
                    )
                )
            );
        }

        return Task.CompletedTask;
    }


    private Task ConfigureUserMenuAsync(MenuConfigurationContext context)
    {
        var accountStringLocalizer = context.GetLocalizer<AccountResource>();

        var authServerUrl = _configuration["AuthServer:Authority"] ?? "";

        context.Menu.AddItem(new ApplicationMenuItem(
            "Account.Manage",
            accountStringLocalizer["MyAccount"],
            $"{authServerUrl.EnsureEndsWith('/')}Account/Manage",
            icon: "fa fa-cog",
            order: 1000,
            target: "_blank").RequireAuthenticated());

        return Task.CompletedTask;
    }
}
