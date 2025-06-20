using System.Linq.Expressions;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for the <see cref="MappingUtils.BuildMemberMap{TSource,TDestination}"/> helper method.
/// </summary>
[TestFixture]
public class MappingUtilsTests
{
    // --- Test-specific dummy types ---
    private class Source
    {
        public int    Id   { get; set; }
        public string? Name { get; set; }
    }

    private class Dest
    {
        public int    EntityId { get; set; }
        public string? FullName { get; set; }
    }

    /// <summary>
    /// Verifies that a simple, direct one-to-one property mapping expression
    /// correctly builds a dictionary of source-to-destination member names.
    /// </summary>
    [Test]
    public void BuildMemberMap_WithSimpleMapping_ShouldReturnCorrectDictionary()
    {
        // Arrange
        // The mapping expression uses a member-init expression with direct assignments.
        Expression<Func<Source, Dest>> mapping =
            src => new Dest
            {
                EntityId = src.Id,
                FullName = src.Name
            };

        // Act
        var dict = MappingUtils.BuildMemberMap(mapping);

        // Assert
        Assert.That(dict, Has.Count.EqualTo(2));
        Assert.That(dict["Id"], Is.EqualTo("EntityId"));
        Assert.That(dict["Name"], Is.EqualTo("FullName"));
    }

    /// <summary>
    /// Verifies that providing a mapping expression that is not a MemberInitExpression
    /// (e.g., a simple 'new' expression) throws an InvalidOperationException.
    /// </summary>
    [Test]
    public void BuildMemberMap_WithNonMemberInitExpression_ShouldThrowInvalidOperationException()
    {
        // Arrange
        // This expression is a NewExpression, not a MemberInitExpression, because it lacks an object initializer.
        Expression<Func<Source, Dest>> mapping = src => new Dest();

        // Act & Assert
        Assert.That(() => MappingUtils.BuildMemberMap(mapping), Throws.InstanceOf<InvalidOperationException>());
    }

    /// <summary>
    /// Verifies that the builder correctly ignores assignments that are not direct
    /// member accesses (e.g., those involving operators or method calls).
    /// </summary>
    [Test]
    public void BuildMemberMap_ShouldIgnoreNonDirectMemberAssignments()
    {
        // Arrange
        // 'FullName' uses the null-coalescing operator, which is a BinaryExpression, not a direct MemberExpression.
        Expression<Func<Source, Dest>> mapping =
            src => new Dest
            {
                EntityId = src.Id,
                FullName = src.Name ?? "N/A"
            };

        // Act
        var dict = MappingUtils.BuildMemberMap(mapping);

        // Assert
        Assert.That(dict, Has.Count.EqualTo(1));
        Assert.That(dict, Does.ContainKey("Id"), "Should map the direct member access.");
        Assert.That(dict["Id"], Is.EqualTo("EntityId"));
        Assert.That(dict, Does.Not.ContainKey("Name"), "Bindings that are not simple member accesses should be skipped.");
    }
}
