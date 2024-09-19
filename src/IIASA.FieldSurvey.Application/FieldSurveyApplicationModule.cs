using IIASA.FieldSurvey.core;
using IIASA.FieldSurvey.services;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace IIASA.FieldSurvey;

[DependsOn(
    typeof(FieldSurveyDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(FieldSurveyApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class FieldSurveyApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<FieldSurveyApplicationModule>();
        });
        context.Services.AddScoped<IAzureFileUploader, AzureFileUploader>();
        context.Services.AddScoped<IYomaTokenManager, YomaTokenManager>();
    }
}
