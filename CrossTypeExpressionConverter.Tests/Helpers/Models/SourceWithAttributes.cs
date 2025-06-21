using CrossTypeExpressionConverter;

namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SourceWithAttributes
{
    [MapsTo(nameof(DestAttributeTarget.IdAlias))]
    public int Id { get; set; }

    [MapsTo(nameof(DestAttributeTarget.NameAlias))]
    public string? Name { get; set; }

    public bool IsActive { get; set; }
}

