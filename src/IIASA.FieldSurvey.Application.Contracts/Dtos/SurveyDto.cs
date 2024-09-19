using IIASA.FieldSurvey.Enum;
using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class SurveyDto : EntityDto<long>
{
    public Coordinate[] Location { get; set; }

    public string Data { get; set; }
    
    public ReviewStatus Status { get; set; }
}