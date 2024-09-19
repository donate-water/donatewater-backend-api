using System;
using System.Collections.Generic;
using IIASA.FieldSurvey.Enum;
using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class SurveyMapDto : EntityDto<long>
{
    public ReviewStatus Status { get; set; } = ReviewStatus.NotReviewed;
    public DateTime CreationTime { get; set; }
    public Coordinate[] Location { get; set; }

    public ImageDto[] Images { get; set; }

    public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
}