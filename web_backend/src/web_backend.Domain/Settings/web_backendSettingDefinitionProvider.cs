using Volo.Abp.Settings;

namespace web_backend.Settings;

public class web_backendSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(web_backendSettings.MySetting1));
    }
}
