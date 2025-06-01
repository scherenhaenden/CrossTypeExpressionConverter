namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class SubjectDatabaseModel // Source for "Find" like scenario
{
    public int SubjectKey { get; set; }
    public string? SubjectName { get; set; }
    public bool IsMandatory { get; set; }
}