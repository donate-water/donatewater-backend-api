using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace IIASA.FieldSurvey.OpenIddict;

public static class IdentityUserExtensions
{
    private const string UserCryptoAccountPropertyName = "UserCryptoAccount";

    public static void SetUserCryptoAccount(this IdentityUser user, string accountLink)
    {
        user.SetProperty(UserCryptoAccountPropertyName, accountLink);
    }

    public static string GetUserCryptoAccount(this IdentityUser user)
    {
        return user.GetProperty(UserCryptoAccountPropertyName,string.Empty);
    }
}