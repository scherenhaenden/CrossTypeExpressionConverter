using CrossTypeExpressionConverter;

namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SourceWithAttributeId
{
    [MapsTo(nameof(DestForCustomMap.Id))]
    public int Id { get; set; }
}
