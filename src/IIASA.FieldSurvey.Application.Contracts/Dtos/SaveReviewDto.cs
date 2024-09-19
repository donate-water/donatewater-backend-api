using IIASA.FieldSurvey.Enum;

namespace IIASA.FieldSurvey.Dtos;

public class SaveReviewDto
{
    public int? ReviewId { get; set; }
    public ReviewStatus Status { get; set; }
    public string Comment { get; set; }
    
    public int SurveyRating { get; set; } = 1; // (low)1-10(high quality)
}