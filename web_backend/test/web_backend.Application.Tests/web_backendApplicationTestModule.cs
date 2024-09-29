using Volo.Abp.Modularity;

namespace web_backend;

[DependsOn(
    typeof(web_backendApplicationModule),
    typeof(web_backendDomainTestModule)
)]
public class web_backendApplicationTestModule : AbpModule
{

}
