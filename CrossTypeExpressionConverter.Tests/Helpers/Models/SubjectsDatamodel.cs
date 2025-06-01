namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SubjectsDatamodel // Destination for "Find" like scenario
{
    public int SubjectKey { get; set; } // Same name
    public string? NameOfSubject { get; set; } // Different name
    public bool Mandatory { get; set; } // Different name
}