using IIASA.FieldSurvey.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace IIASA.FieldSurvey.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(FieldSurveyEntityFrameworkCoreModule),
    typeof(FieldSurveyApplicationContractsModule)
    )]
public class FieldSurveyDbMigratorModule : AbpModule
{

}
