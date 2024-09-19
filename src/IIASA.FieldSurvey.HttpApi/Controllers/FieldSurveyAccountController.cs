using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;

namespace IIASA.FieldSurvey.Controllers;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(AccountController))]
public class FieldSurveyAccountController : AccountController
{
    public FieldSurveyAccountController(IAccountAppService accountAppService)
        : base(accountAppService)
    {

    }

    [HttpPost]
    [Route("dummy/[action]", Order = 0, Name = "Replacement")]  // for swagger to ignore
    public override async Task<Volo.Abp.Identity.IdentityUserDto> RegisterAsync(RegisterDto input)
    {
        try
        {
            var result = await base.RegisterAsync(input);

            return new IdentityUserDto().Init(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new IdentityUserDto {ErrorMessage = e.Message, IsValid = false};
        }
    }

    [HttpPost]
    [Route("dummy/send-password-reset-code", Order = 0)]
    public override async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        await AccountAppService.SendPasswordResetCodeAsync(new SendPasswordResetCodeDto {Email = input.Email, AppName = "MVC"});
    }

    [HttpPost]
    [Route("dummy/reset-password", Order = 0)]
    public override async Task ResetPasswordAsync(ResetPasswordDto input)
    {
        await AccountAppService.ResetPasswordAsync(input);
    }
}