using System;
using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class UserScore
{
    public string UserName { get; set; }
    
    public string UserEmail { get; set; }
    public Guid UserId { get; set; }
    public int SurveyCount { get; set; }
    public int AcceptedSurveyCount { get; set; }
    public int RejectedSurveyCount { get; set; }
    public int Score { get; set; }
    public int TotalImagesUploadedCount { get; set; }
    public int Rank { get; set; }
}

public class PayoutDto : CreationAuditedEntityDto<long>
{
    public int TokensPaid{ get; set; }

    public Guid UserId { get; set; }
    
    public int DeductedScore { get; set; }
}

public class PayoutRequest
{
    public int TokensPaid{ get; set; }
}

public class CryptoAccount
{
    public string Address { get; set; } = string.Empty;
}