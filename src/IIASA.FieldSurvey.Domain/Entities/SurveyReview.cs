using System;
using IIASA.FieldSurvey.Enum;
using Volo.Abp.Domain.Entities.Auditing;

namespace IIASA.FieldSurvey.Entities;

public class SurveyReview : FullAuditedEntity<long>
{
    public ReviewStatus Status { get; set; }

    public string Comments { get; set; }

    public Survey Survey { get; set; }

    public int Score { get; set; }

    public int SurveyRating { get; set; } = 1; // (low)1-10(high quality)
}

public class Payout : CreationAuditedEntity<long>
{
    public int TokensPaid{ get; set; }

    public Guid UserId { get; set; }
    
    public int DeductedScore { get; set; }
}