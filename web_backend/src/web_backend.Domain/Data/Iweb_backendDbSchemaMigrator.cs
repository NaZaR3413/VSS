using System.Threading.Tasks;

namespace web_backend.Data;

public interface Iweb_backendDbSchemaMigrator
{
    Task MigrateAsync();
}
