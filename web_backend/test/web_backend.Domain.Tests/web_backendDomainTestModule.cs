using Volo.Abp.Modularity;

namespace web_backend;

[DependsOn(
    typeof(web_backendDomainModule),
    typeof(web_backendTestBaseModule)
)]
public class web_backendDomainTestModule : AbpModule
{

}
