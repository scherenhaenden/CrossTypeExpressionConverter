namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SourceWithNested
{
    public int ParentId { get; set; }
    public NestedSourceProp? Child { get; set; }
    public NestedSourceProp? ChildToMap { get; set; }
}