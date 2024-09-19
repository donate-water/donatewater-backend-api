using IIASA.FieldSurvey.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace IIASA.FieldSurvey.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class FieldSurveyController : AbpControllerBase
{
    protected FieldSurveyController()
    {
        LocalizationResource = typeof(FieldSurveyResource);
    }
}
