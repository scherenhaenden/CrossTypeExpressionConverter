using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for the conversion of nested properties.
/// </summary>
[TestFixture]
public class NestedPropertyMappingTests
{
    /// <summary>
    /// Compiles and executes a predicate against an item, returning the boolean result.
    /// </summary>
    /// <param name="predicate">The expression predicate to evaluate.</param>
    /// <param name="item">The object to test the predicate against.</param>


    /// <summary>
    /// Verifies that a predicate on a nested property is correctly converted when the property names are the same.
    /// </summary>
    [Test]
    public void Convert_NestedProperty_WithSameNames_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.Child != null && s.Child.NestedId == 100;

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceWithNested, DestWithNested>(sourcePredicate);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 100 } }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 50 } }), Is.False);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestWithNested { Child = null }), Is.False);
    }

    /// <summary>
    /// Verifies that a deeply nested property can be correctly mapped using a customMap delegate,
    /// especially when both parent and child property names differ.
    /// </summary>
    [Test]
    public void Convert_DeeplyNestedProperty_WithCustomMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        // The goal is to map s.ChildToMap.NestedName to d.MappedChild.InnerName
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.ChildToMap!.NestedName == "DeepMap";

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (srcMemberExpr, destParamExpr) =>
        {
            // Detect the full expression path: s.ChildToMap.NestedName
            if (srcMemberExpr.Expression is MemberExpression parentMemberExpr &&
                parentMemberExpr.Member.Name == nameof(SourceWithNested.ChildToMap) &&
                srcMemberExpr.Member.Name == nameof(NestedSourceProp.NestedName))
            {
                // Build the destination path: d.MappedChild
                var mappedChildPropInfo = typeof(DestWithNested).GetProperty(nameof(DestWithNested.MappedChild));
                var destMappedChildAccess = Expression.Property(destParamExpr, mappedChildPropInfo!);

                // Build the final path: d.MappedChild.InnerName
                var innerNamePropInfo = typeof(NestedDestPropDifferentName).GetProperty(nameof(NestedDestPropDifferentName.InnerName));
                return Expression.Property(destMappedChildAccess, innerNamePropInfo!);
            }
            return null;
        };
        
        var options = new ExpressionConverterOptions().WithCustomMap(customMap);

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceWithNested, DestWithNested>(sourcePredicate, options);
        
        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "DeepMap" } }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "Wrong" } }), Is.False);
    }
}
