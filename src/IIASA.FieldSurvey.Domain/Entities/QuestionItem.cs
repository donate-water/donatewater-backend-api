using Volo.Abp.Domain.Entities;

namespace IIASA.FieldSurvey.Entities;

public class QuestionItem: Entity<int>
{
    public string LangCode { get; set; } = "en";
    public string Key { get; set; }
    
    public string Type { get; set; }
    public string Question { get; set; }
    public int Order { get; set; }
}