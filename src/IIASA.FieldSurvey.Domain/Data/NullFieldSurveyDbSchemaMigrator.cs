using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace IIASA.FieldSurvey.Data;

/* This is used if database provider does't define
 * IFieldSurveyDbSchemaMigrator implementation.
 */
public class NullFieldSurveyDbSchemaMigrator : IFieldSurveyDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
