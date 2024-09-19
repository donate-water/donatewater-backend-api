using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IIASA.FieldSurvey.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IIASA.FieldSurvey.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Azure.Storage;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.AntiForgery;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace IIASA.FieldSurvey;

[DependsOn(
    typeof(FieldSurveyHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(FieldSurveyApplicationModule),
    typeof(FieldSurveyEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class FieldSurveyHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("FieldSurvey");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
            builder.AddServer(options => { options.UseAspNetCore().DisableTransportSecurityRequirement(); });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context, configuration);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        Configure<AbpAntiForgeryOptions>(options =>
        {
            options.TokenCookie.Expiration = TimeSpan.FromDays(365);
            bool v = options.AutoValidateIgnoredHttpMethods.Remove("GET");
            /*options.AutoValidateFilter =
                type => !type.Namespace.StartsWith("IIASA.");*/
            options.AutoValidateFilter = type => false;
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults
            .AuthenticationScheme);

        AzureConfig azureConfig = new AzureConfig();
        configuration.GetSection("AzureConfig").Bind(azureConfig);

        var config = new ScoreConfig
        {
            RatingToScoreFactor = int.Parse(configuration["ScoreConfig:RatingToScoreFactor"]!),
             ScoreToTokenFactor= double.Parse(configuration["ScoreConfig:ScoreToTokenFactor"]!),
             MinScoreForPayout = int.Parse(configuration["ScoreConfig:MinScoreForPayout"]!),
             EnableTokensPayout = bool.Parse(configuration["ScoreConfig:EnableTokensPayout"]!)
        };

        context.Services.AddSingleton(config);

        var yomaTokenConfig = new YomaTokenConfig
        {
            ServiceUrl = configuration["YomaTokenConfig:ServiceUrl"],
            AppOwnerPublicKey = configuration["YomaTokenConfig:AppOwnerPublicKey"],
            AppOwnerPrivateKey = configuration["YomaTokenConfig:AppOwnerPrivateKey"],
            ContractAddress = configuration["YomaTokenConfig:ContractAddress"],
            ProjectWalletAddress = configuration["YomaTokenConfig:ProjectWalletAddress"],
            ChainId = int.Parse(configuration["YomaTokenConfig:ChainId"]!),
            AppCredentials = configuration["YomaTokenConfig:AppCredentials"],
        };
        context.Services.AddSingleton(yomaTokenConfig);

        var cloudStorageAccount = CloudStorageAccount.Parse(azureConfig.StorageConnectionStrings);
        context.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(cloudStorageAccount,
                azureConfig.DataProtectionContainer + "/" + azureConfig.DataProtectionFileName);

        context.Services.AddSingleton(azureConfig);
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle => { bundle.AddFiles("/global-styles.css"); }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"].Split(','));

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<FieldSurveyDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}IIASA.FieldSurvey.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<FieldSurveyDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}IIASA.FieldSurvey.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<FieldSurveyApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}IIASA.FieldSurvey.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<FieldSurveyApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}IIASA.FieldSurvey.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(FieldSurveyApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSwaggerGen(
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "FieldSurvey API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.DocumentFilter<ApiDocumentFilter>();
                options.ResolveConflictingActions(Extract());
                options.AddSecurityDefinition("Bearer", SecurityScheme(configuration));
                options.AddSecurityRequirement(SecurityRequirement());
            });
    }

    private static Func<IEnumerable<ApiDescription>, ApiDescription> Extract()
    {
        return apiDescriptions => apiDescriptions.First();
    }

    private static OpenApiSecurityScheme SecurityScheme(IConfiguration configuration)
    {
        return new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below. Get the token from POST https://<domain>/connect/token
                      Example: 'Bearer eyJhbGciOiJSUzI1NiIsImtpZ...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            OpenIdConnectUrl = new Uri(configuration["App:SelfUrl"]),
        };
    }

    private static OpenApiSecurityRequirement SecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        };
    }


    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        ConfigureIdentity(context, app);
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        app.UseUnitOfWork();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FieldSurvey API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("FieldSurvey");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }

    private void ConfigureIdentity(ApplicationInitializationContext context, IApplicationBuilder app)
    {
        var forwardOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            RequireHeaderSymmetry = false
        };
        forwardOptions.KnownNetworks.Clear();
        forwardOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardOptions);
        app.Use((httpContext, next) =>
        {
            httpContext.Request.Scheme = "https";
            return next();
        });
    }
}