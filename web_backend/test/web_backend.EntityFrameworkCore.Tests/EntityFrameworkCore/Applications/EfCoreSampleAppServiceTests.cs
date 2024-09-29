using web_backend.Samples;
using Xunit;

namespace web_backend.EntityFrameworkCore.Applications;

[Collection(web_backendTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<web_backendEntityFrameworkCoreTestModule>
{

}
