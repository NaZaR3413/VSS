using web_backend.Samples;
using Xunit;

namespace web_backend.EntityFrameworkCore.Domains;

[Collection(web_backendTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<web_backendEntityFrameworkCoreTestModule>
{

}
