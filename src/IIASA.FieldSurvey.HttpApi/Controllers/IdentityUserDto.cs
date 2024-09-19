namespace IIASA.FieldSurvey.Controllers
{
    public class IdentityUserDto : Volo.Abp.Identity.IdentityUserDto
    {
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; }

        public IdentityUserDto Init(Volo.Abp.Identity.IdentityUserDto result)
        {
            Id = result.Id;
            Name = result.Name;
            ConcurrencyStamp = result.ConcurrencyStamp;
            Email = result.Email;
            EmailConfirmed = result.EmailConfirmed;
            LockoutEnabled = result.LockoutEnabled;
            LockoutEnd = result.LockoutEnd;
            PhoneNumber = result.PhoneNumber;
            PhoneNumberConfirmed = result.PhoneNumberConfirmed;
            TenantId = result.TenantId;
            CreationTime = result.CreationTime;
            CreatorId = result.CreatorId;
            return this;
        }
    }
}