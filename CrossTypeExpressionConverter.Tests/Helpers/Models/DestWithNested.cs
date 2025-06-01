namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class DestWithNested
{
    public int ParentId { get; set; }
    public NestedDestProp? Child { get; set; } // Same name for nested object property
    public NestedDestPropDifferentName? MappedChild { get; set; } // Different name for nested object property
}