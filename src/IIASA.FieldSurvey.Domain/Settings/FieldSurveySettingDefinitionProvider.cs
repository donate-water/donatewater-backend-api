using Volo.Abp.Settings;

namespace IIASA.FieldSurvey.Settings;

public class FieldSurveySettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(FieldSurveySettings.MySetting1));
    }
}
