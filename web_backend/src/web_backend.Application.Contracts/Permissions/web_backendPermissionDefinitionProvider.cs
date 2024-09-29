using web_backend.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace web_backend.Permissions;

public class web_backendPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(web_backendPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(web_backendPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<web_backendResource>(name);
    }
}
