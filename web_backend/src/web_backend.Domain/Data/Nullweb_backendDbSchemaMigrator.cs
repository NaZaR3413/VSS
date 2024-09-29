using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace web_backend.Data;

/* This is used if database provider does't define
 * Iweb_backendDbSchemaMigrator implementation.
 */
public class Nullweb_backendDbSchemaMigrator : Iweb_backendDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
