using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for miscellaneous functionalities of the converter,
/// such as handling of expression parameter names.
/// </summary>
[TestFixture]
public class MiscellaneousTests
{
    /// <summary>
    /// Verifies that the converter preserves the original parameter name from the source lambda expression.
    /// </summary>
    [Test]
    public void Convert_ShouldPreserveSourceParameterName_IfExists()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = item => item.Id == 1; // The parameter name is "item".

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceSimple, DestSimple>(sourcePredicate);
        
        // Assert
        Assert.That(convertedPredicate.Parameters[0].Name, Is.EqualTo("item"));
    }

    /// <summary>
    /// Verifies that the converter uses a generated or default parameter name when the source expression does not have an explicit one.
    /// </summary>
    [Test]
    public void Convert_ShouldUseDefaultParameterName_IfSourceHasNone()
    {
        // Arrange
        // Manually create an expression without an explicit parameter name.
        var param = Expression.Parameter(typeof(SourceSimple)); 
        var body = Expression.Equal(Expression.Property(param, nameof(SourceSimple.Id)), Expression.Constant(1));
        var sourcePredicate = Expression.Lambda<Func<SourceSimple, bool>>(body, param);

        // The converter will use the compiler-generated name if one exists, or "p" as a fallback.
        var expectedName = param.Name ?? "p";

        // Act
        var converted = ExpressionConverterFacade.Convert<SourceSimple, DestSimple>(sourcePredicate);
        
        // Assert
        Assert.That(converted.Parameters[0].Name, Is.EqualTo(expectedName));
    }
}