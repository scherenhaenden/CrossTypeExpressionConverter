namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class DestForCustomMap
{
    public int Id { get; set; }
    public string? TransformedData { get; set; } // Data will be mapped here by custom logic
    public int DoubledValue { get; set; } // NumericValue will be mapped and transformed here
}