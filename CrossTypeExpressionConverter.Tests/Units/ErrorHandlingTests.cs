using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for the error handling capabilities of the ExpressionConverter,
/// specifically how it behaves when members cannot be mapped.
/// </summary>
[TestFixture]
public class ErrorHandlingTests
{
    /// <summary>
    /// Compiles and executes a predicate against an item, returning the boolean result.
    /// </summary>
    /// <param name="predicate">The expression predicate to evaluate.</param>
    /// <param name="item">The object to test the predicate against.</param>
    /// <returns>The result of the predicate evaluation.</returns>
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    /// <summary>
    /// Verifies that the converter throws an InvalidOperationException by default when a member does not exist on the destination type.
    /// </summary>
    [Test]
    public void Convert_MemberMissingOnDestination_ShouldThrow_ByDefault()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "Test";
        // By default, ErrorHandling is 'Throw'.
        var options = ExpressionConverterOptions.Default; 

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options)
        );
        Assert.That(ex?.Message, Does.Contain(nameof(SourceSimple.PropertyToIgnoreOnDest)));
    }
    
    /// <summary>
    /// Verifies that the converter throws an InvalidOperationException by default when a member specified in the MemberMap does not exist on the destination type.
    /// </summary>
    [Test]
    public void Convert_MemberInMapMissingOnDestination_ShouldThrow_ByDefault()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Id), "NonExistentDestProperty" } });
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options)
        );
        Assert.That(ex?.Message, Does.Contain("NonExistentDestProperty"));
    }

    /// <summary>
    /// Verifies that the converter does not throw an exception and returns a default value when a member is missing, if ErrorHandling is set to ReturnDefault.
    /// </summary>
    [Test]
    public void Convert_MemberMissingOnDestination_WithReturnDefault_ShouldEvaluateToFalse()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault);
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "Test";

        // Act
        var converted = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options);

        // Assert: The predicate becomes (null == "Test"), which is false.
        Assert.That(Evaluate(converted, new DestSimple()), Is.False);
    }

    /// <summary>
    /// Verifies that the converter does not throw and returns a default value when a mapped member is missing, if ErrorHandling is set to ReturnDefault.
    /// </summary>
    [Test]
    public void Convert_MemberInMapMissingOnDestination_WithReturnDefault_ShouldEvaluateToFalse()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault)
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Id), "NonExistentDestProperty" } });
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1;

        // Act
        var converted = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options);
        
        // Assert: The predicate becomes (0 == 1), which is false.
        Assert.That(Evaluate(converted, new DestSimple { Id = 99 }), Is.False);
    }

    /// <summary>
    /// Verifies that in a compound logical expression (OR), if one part fails to map but ReturnDefault is enabled,
    /// the rest of the expression is still evaluated correctly.
    /// </summary>
    [Test]
    public void Convert_CompoundExpressionWithMissingMember_WithReturnDefault_ShouldEvaluateCorrectly()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault);
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "A" || s.IsActive;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options);

        // Assert: The predicate becomes (null == "A" || d.IsActive), which is equivalent to (false || d.IsActive).
        // It should be true if IsActive is true.
        Assert.That(Evaluate(convertedPredicate, new DestSimple { IsActive = true }), Is.True);
        Assert.That(Evaluate(convertedPredicate, new DestSimple { IsActive = false }), Is.False);
    }
}
