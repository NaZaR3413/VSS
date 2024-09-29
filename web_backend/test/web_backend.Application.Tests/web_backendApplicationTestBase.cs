using Volo.Abp.Modularity;

namespace web_backend;

public abstract class web_backendApplicationTestBase<TStartupModule> : web_backendTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
