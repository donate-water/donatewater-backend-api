using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class QuestionDto : EntityDto<int>
{
    public string Key { get; set; }
    public string Question { get; set; }
    public string Type { get; set; }
    public int Order { get; set; }
}