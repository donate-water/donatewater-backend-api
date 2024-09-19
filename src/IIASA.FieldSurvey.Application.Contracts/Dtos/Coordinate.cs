using System;

namespace IIASA.FieldSurvey.Dtos;

public class Coordinate
{
    public double YLat { get; set; }

    public double XLng { get; set; }
}

public class ReviewerDto
{
    public string UserName { get; set; }
    public string UserEmailId { get; set; }
}