using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests to verify the preservation of the expression's structure,
/// such as logical operators and method calls, during conversion.
/// </summary>
[TestFixture]
public class ExpressionStructureTests
{
    /// <summary>
    /// Compiles and executes a predicate against an item, returning the boolean result.
    /// </summary>
    /// <param name="predicate">The expression predicate to evaluate.</param>
    /// <param name="item">The object to test the predicate against.</param>
    /// <returns>The result of the predicate evaluation.</returns>


    // --- Boolean Logic Tests ---

    /// <summary>
    /// Verifies that a compound expression with an 'AND' logical operator is correctly converted.
    /// </summary>
    [Test]
    public void Convert_AndLogic_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 5 && s.IsActive;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = true }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 3, IsActive = true }), Is.False);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = false }), Is.False);
    }

    /// <summary>
    /// Verifies that a compound expression with an 'OR' logical operator and a member map is correctly converted.
    /// </summary>
    [Test]
    public void Convert_OrLogic_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1 || s.Name == "Valid";
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string>
            {
                { nameof(SourceSimple.Id), nameof(DestDifferentNames.EntityId) },
                { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) }
            });

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 1, FullName = "Other" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Valid" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Other" }), Is.False);
    }

    // --- Method Call Tests ---

    /// <summary>
    /// Verifies that a predicate containing a 'StartsWith' method call is correctly converted.
    /// </summary>
    [Test]
    public void Convert_StringStartsWith_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name != null && s.Name.StartsWith("Prefix");

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Name = "PrefixValue" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Name = "NoPrefix" }), Is.False);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Name = null }), Is.False);
    }

    /// <summary>
    /// Verifies that a predicate with a 'Contains' method call and a member map is correctly converted.
    /// </summary>
    [Test]
    public void Convert_StringContains_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name != null && s.Name.Contains("Middle");
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) } });

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { FullName = "StartMiddleEnd" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { FullName = "NoMatch" }), Is.False);
    }
    
    // --- Constant Value Test ---

    /// <summary>
    /// Verifies that constant expressions within the predicate are preserved during conversion.
    /// </summary>
    [Test]
    public void Convert_WithConstantComparison_ShouldPreserveConstantLogic()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 0 && 10 > 5; // 10 > 5 is always true

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 1 }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = -1 }), Is.False);
    }
}
