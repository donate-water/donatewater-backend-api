namespace IIASA.FieldSurvey.Dtos;

public class SurveyQnA
{
    public string Question { get; set; }
    public string Answer { get; set; }
    public string OtherAnswer { get; set; }
    public int Order { get; set; }
    public string Key { get; set; }
    public string Type { get; set; }
    public string LangCode { get; set; }
}