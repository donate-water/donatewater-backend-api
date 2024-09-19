using IIASA.FieldSurvey.Enum;
using NetTopologySuite.Geometries;
using Volo.Abp.Domain.Entities.Auditing;

namespace IIASA.FieldSurvey.Entities;

public class Survey : FullAuditedEntity<long>
{
    public Geometry Location { get; set; }

    public string Data { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.NotReviewed;
}