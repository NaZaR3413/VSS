using Xunit;

namespace web_backend.EntityFrameworkCore;

[CollectionDefinition(web_backendTestConsts.CollectionDefinitionName)]
public class web_backendEntityFrameworkCoreCollection : ICollectionFixture<web_backendEntityFrameworkCoreFixture>
{

}
