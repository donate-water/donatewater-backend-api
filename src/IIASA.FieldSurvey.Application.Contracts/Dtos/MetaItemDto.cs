using Volo.Abp.Application.Dtos;

namespace IIASA.FieldSurvey.Dtos;

public class MetaItemDto : EntityDto<int>
{
    public string Key { get; set; }

    public int Index { get; set; }

    public string IndexValue { get; set; }
}