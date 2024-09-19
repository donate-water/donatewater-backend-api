using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IIASA.FieldSurvey.Data;
using Volo.Abp.DependencyInjection;

namespace IIASA.FieldSurvey.EntityFrameworkCore;

public class EntityFrameworkCoreFieldSurveyDbSchemaMigrator
    : IFieldSurveyDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreFieldSurveyDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the FieldSurveyDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<FieldSurveyDbContext>()
            .Database
            .MigrateAsync();
    }
}
