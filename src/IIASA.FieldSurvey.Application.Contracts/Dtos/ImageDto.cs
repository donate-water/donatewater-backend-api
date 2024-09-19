using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class ImageDto : EntityDto<long>
{
    public string StorageUrl { get; set; }

    public string Data { get; set; }
}