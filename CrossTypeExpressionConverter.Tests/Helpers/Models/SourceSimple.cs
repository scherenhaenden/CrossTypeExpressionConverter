namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

 // --- Test Model Classes ---
    public class SourceSimple
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public double Value { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PropertyToIgnoreOnDest { get; set; }
    }

    // --- Models for User's Scenarios ---