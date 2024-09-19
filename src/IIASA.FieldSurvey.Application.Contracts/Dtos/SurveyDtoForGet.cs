using System;

namespace IIASA.FieldSurvey.Dtos;

public class SurveyDtoForGet : SurveyDto
{
    public DateTime CreationTime { get; set; }
    public ImageDto[] Images { get; set; }
}