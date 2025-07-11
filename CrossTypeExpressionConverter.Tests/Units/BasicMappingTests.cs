using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for the basic mapping functionality of the ExpressionConverter,
/// covering same-name property matching and dictionary-based mapping.
/// </summary>
[TestFixture]
public class BasicMappingTests
{


    // --- Same-Name Property Mapping Tests ---

    /// <summary>
    /// Verifies that a predicate on an integer property is correctly converted when property names match between source and destination.
    /// </summary>
    [Test]
    public void Convert_SimpleIntProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 10;

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 10 }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Id = 5 }), Is.False);
    }

    /// <summary>
    /// Verifies that a predicate on a string property is correctly converted when property names match.
    /// </summary>
    [Test]
    public void Convert_SimpleStringProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "Test";

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Name = "Test" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { Name = "Other" }), Is.False);
    }

    /// <summary>
    /// Verifies that a predicate on a boolean property is correctly converted when property names match.
    /// </summary>
    [Test]
    public void Convert_SimpleBoolProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.IsActive; // Equivalent to s.IsActive == true

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { IsActive = true }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestSimple { IsActive = false }), Is.False);
    }

    // --- Dictionary-Based (MemberMap) Mapping Tests ---

    /// <summary>
    /// Verifies that a predicate on an integer property is correctly converted using a member map for different property names.
    /// </summary>
    [Test]
    public void Convert_IntProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 20;
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Id), nameof(DestDifferentNames.EntityId) } });

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 20 }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 15 }), Is.False);
    }

    /// <summary>
    /// Verifies that a predicate on a string property is correctly converted using a member map for different property names.
    /// </summary>
    [Test]
    public void Convert_StringProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "MapTest";
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) } });

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { FullName = "MapTest" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestDifferentNames { FullName = "OtherMap" }), Is.False);
    }
}
