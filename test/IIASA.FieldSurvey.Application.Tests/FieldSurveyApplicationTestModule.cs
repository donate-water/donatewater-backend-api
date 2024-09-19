using Volo.Abp.Modularity;

namespace IIASA.FieldSurvey;

[DependsOn(
    typeof(FieldSurveyApplicationModule),
    typeof(FieldSurveyDomainTestModule)
    )]
public class FieldSurveyApplicationTestModule : AbpModule
{

}
