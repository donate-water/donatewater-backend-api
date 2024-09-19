using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using NetTopologySuite.Geometries;
using Volo.Abp.AutoMapper;
using Coordinate = IIASA.FieldSurvey.Dtos.Coordinate;

namespace IIASA.FieldSurvey;

public class FieldSurveyApplicationAutoMapperProfile : Profile
{
    public FieldSurveyApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Survey, SurveyDtoForGet>().Ignore(x => x.Images)
            .ForMember(x => x.Location, y => y
                .MapFrom(c => GetCoordinates(c.Location.Coordinates)));

        CreateMap<Survey, SurveyMapDto>().Ignore(x => x.Images).Ignore(x => x.Data)
            .ForMember(x => x.Location, y => y
                .MapFrom(c => GetCoordinates(c.Location.Coordinates)));

        CreateMap<SurveyDto, Survey>()
            .ForMember(x => x.Location, expression => expression.MapFrom(s => GetLocation(s.Location)));
        CreateMap<MetaItem, MetaItemDto>().ReverseMap();
        CreateMap<QuestionItem, QuestionDto>().ReverseMap();

        CreateMap<PayoutDto, Payout>().ReverseMap();
    }
    private Geometry GetLocation(Coordinate[] coordinates)
    {
        if (coordinates.Length > 1)
        {
            var points =
                coordinates.Select(x => new NetTopologySuite.Geometries.Coordinate { X = x.XLng, Y = x.YLat })
                    .ToArray();
            return new Polygon(new LinearRing(points),
                new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326));
        }

        return new Point(coordinates[0].XLng, coordinates[0].YLat);
    }
    
    public static IEnumerable<Coordinate> GetCoordinates(IEnumerable<NetTopologySuite.Geometries.Coordinate> locationCoordinates)
    {
        return locationCoordinates.Select(s => new Coordinate { XLng = s.X, YLat = s.Y });
    }
}
