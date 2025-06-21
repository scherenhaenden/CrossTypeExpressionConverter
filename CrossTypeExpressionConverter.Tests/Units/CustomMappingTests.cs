using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for the custom mapping functionality using the 'customMap' delegate.
/// </summary>
[TestFixture]
public class CustomMappingTests
{


    /// <summary>
    /// Verifies that a string property can be mapped to a destination property with a different name using a custom map.
    /// </summary>
    [Test]
    public void Convert_StringProperty_WithCustomMap_ToDifferentProperty()
    {
        // Arrange
        Expression<Func<SourceForCustomMap, bool>> sourcePredicate = s => s.Data == "PREFIX_Original";

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (sourceMemberExpr, destParamExpr) =>
        {
            if (sourceMemberExpr.Member.Name == nameof(SourceForCustomMap.Data))
            {
                var transformedDataProp = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.TransformedData));
                if (transformedDataProp == null) throw new NullReferenceException("TransformedData property not found");
                return Expression.Property(destParamExpr, transformedDataProp);
            }
            return null; // Fallback for other members
        };

        var options = new ExpressionConverterOptions().WithCustomMap(customMap);

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "PREFIX_Original" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "Original" }), Is.False);
    }
    
    /// <summary>
    /// Verifies that a numeric property can be mapped to a transformed value on the destination using a custom map.
    /// </summary>
    [Test]
    public void Convert_NumericProperty_WithCustomMap_ToTransformedValue()
    {
        // Arrange
        // The source predicate checks if NumericValue is 10.
        Expression<Func<SourceForCustomMap, bool>> sourcePredicate = s => s.NumericValue == 10;

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (sourceMemberExpr, destParamExpr) =>
        {
            if (sourceMemberExpr.Member.Name == nameof(SourceForCustomMap.NumericValue))
            {
                // We map s.NumericValue to an expression representing 'd.DoubledValue / 2'.
                // This way, the condition 'd.DoubledValue / 2 == 10' becomes 'd.DoubledValue == 20'.
                var doubledValueProp = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.DoubledValue));
                if (doubledValueProp == null) throw new NullReferenceException("DoubledValue property not found");
                
                var destProperty = Expression.Property(destParamExpr, doubledValueProp);
                var twoConstant = Expression.Constant(2);
                return Expression.Divide(destProperty, twoConstant);
            }
            return null;
        };
        
        var options = new ExpressionConverterOptions().WithCustomMap(customMap);

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, options);
        
        // Assert
        // The converted predicate must be true for a destination object where DoubledValue is 20.
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 20 }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 10 }), Is.False);
    }
}
