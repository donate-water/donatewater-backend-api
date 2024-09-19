using Volo.Abp.Domain.Entities.Auditing;

namespace IIASA.FieldSurvey.Entities;

public class Image : AuditedEntity<long>
{
    public long SurveyId { get; set; }
    public string Url { get; set; }

    public string Data { get; set; }
}