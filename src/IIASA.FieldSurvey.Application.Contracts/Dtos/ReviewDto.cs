using System;
using IIASA.FieldSurvey.Enum;

namespace IIASA.FieldSurvey.Dtos;

public class ReviewDto:SurveyCreatorDto
{
    public long Id { get; set; }
    public long SurveyId { get; set; }
    public Coordinate[] Location { get; set; }
    public DateTime SurveyCreationTime { get; set; }
    public ReviewStatus Status { get; set; }
    public int Score { get; set; }
    public int SurveyRating { get; set; } = 1; // (low)1-10(high quality)
    public DateTime CreateOrUpdateTime { get; set; }
    public string LastUpdatedBy { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public string LastUpdatedByEmailId { get; set; }
}

public class SurveyCreatorDto
{
    public Guid? SurveyCreatorId { get; set; }
    public string SurveyCreator { get; set; }
    public string SurveyCreatorEmailId { get; set; }
}