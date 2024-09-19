namespace IIASA.FieldSurvey.Dtos;

public class ReviewDetailsDto : ReviewDto
{
    public string Comment { get; set; }
    public SurveyQnA[] SurveyQandAs { get; set; }
    public ImageDto[] Images { get; set; }
}