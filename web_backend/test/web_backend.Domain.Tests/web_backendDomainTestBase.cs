using Volo.Abp.Modularity;

namespace web_backend;

/* Inherit from this class for your domain layer tests. */
public abstract class web_backendDomainTestBase<TStartupModule> : web_backendTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
