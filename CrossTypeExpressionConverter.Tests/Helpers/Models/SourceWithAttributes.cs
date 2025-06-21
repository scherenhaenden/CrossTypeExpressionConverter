using CrossTypeExpressionConverter;

namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SourceWithAttributes
{
    [MapsTo(nameof(DestDifferentNames.EntityId))]
    public int Id { get; set; }

    [MapsTo(nameof(DestDifferentNames.FullName))]
    public string? Name { get; set; }

    [MapsTo(nameof(DestDifferentNames.Enabled))]
    public bool IsActive { get; set; }
}
