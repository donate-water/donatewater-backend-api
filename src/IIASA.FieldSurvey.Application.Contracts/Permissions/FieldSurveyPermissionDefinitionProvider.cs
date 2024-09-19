using IIASA.FieldSurvey.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace IIASA.FieldSurvey.Permissions;

public class FieldSurveyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(FieldSurveyPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(FieldSurveyPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<FieldSurveyResource>(name);
    }
}
