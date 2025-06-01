namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class NestedDestPropDifferentName
{
    public int InnerId {get; set;} // Mapped from NestedId
    public string? InnerName {get; set;} // Mapped from NestedName
}