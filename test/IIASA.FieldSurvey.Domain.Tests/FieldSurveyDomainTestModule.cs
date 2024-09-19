using IIASA.FieldSurvey.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace IIASA.FieldSurvey;

[DependsOn(
    typeof(FieldSurveyEntityFrameworkCoreTestModule)
    )]
public class FieldSurveyDomainTestModule : AbpModule
{

}
