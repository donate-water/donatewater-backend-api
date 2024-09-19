using System.Threading.Tasks;

namespace IIASA.FieldSurvey.Data;

public interface IFieldSurveyDbSchemaMigrator
{
    Task MigrateAsync();
}
