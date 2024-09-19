using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace IIASA.FieldSurvey;

[Dependency(ReplaceServices = true)]
public class FieldSurveyBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "FieldSurvey";
}
