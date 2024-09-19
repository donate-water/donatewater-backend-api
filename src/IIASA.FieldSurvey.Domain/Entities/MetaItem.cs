using Volo.Abp.Domain.Entities;

namespace IIASA.FieldSurvey.Entities;

public class MetaItem : Entity<int>
{
    public string Key { get; set; }

    public int Index { get; set; }

    public string IndexValue { get; set; }
}