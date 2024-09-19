using System.Text.Json.Serialization;

namespace IIASA.FieldSurvey.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewStatus
{
    NotReviewed,
    Completed,
    Rejected,
    Skipped
}