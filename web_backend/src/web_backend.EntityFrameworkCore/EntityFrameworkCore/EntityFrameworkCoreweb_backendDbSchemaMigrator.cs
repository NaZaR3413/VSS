using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web_backend.Data;
using Volo.Abp.DependencyInjection;

namespace web_backend.EntityFrameworkCore;

public class EntityFrameworkCoreweb_backendDbSchemaMigrator
    : Iweb_backendDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreweb_backendDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the web_backendDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<web_backendDbContext>()
            .Database
            .MigrateAsync();
    }
}
