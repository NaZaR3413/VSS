using web_backend.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace web_backend.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(web_backendEntityFrameworkCoreModule),
    typeof(web_backendApplicationContractsModule)
    )]
public class web_backendDbMigratorModule : AbpModule
{
}
