using IIASA.FieldSurvey.Localization;
using Volo.Abp.Application.Services;

namespace IIASA.FieldSurvey;

/* Inherit your application services from this class.
 */
public abstract class FieldSurveyAppService : ApplicationService
{
    protected FieldSurveyAppService()
    {
        LocalizationResource = typeof(FieldSurveyResource);
    }
}
