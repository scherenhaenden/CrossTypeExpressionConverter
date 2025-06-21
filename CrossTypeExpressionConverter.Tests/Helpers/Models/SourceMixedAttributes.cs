using CrossTypeExpressionConverter;

namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SourceMixedAttributes
{
    [MapsTo(nameof(DestSimple.Id))]
    public int Id { get; set; }

    public string? Name { get; set; }
}
