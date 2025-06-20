using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para la conversión de propiedades anidadas.
/// </summary>
[TestFixture]
public class NestedPropertyMappingTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    [Test]
    public void Convert_NestedProperty_WithSameNames_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.Child != null && s.Child.NestedId == 100;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceWithNested, DestWithNested>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 100 } }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 50 } }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestWithNested { Child = null }));
    }

    [Test]
    public void Convert_DeeplyNestedProperty_WithCustomMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        // El objetivo es mapear s.ChildToMap.NestedName a d.MappedChild.InnerName
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.ChildToMap!.NestedName == "DeepMap";

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (srcMemberExpr, destParamExpr) =>
        {
            // Detecta la expresión completa s.ChildToMap.NestedName
            if (srcMemberExpr.Expression is MemberExpression parentMemberExpr &&
                parentMemberExpr.Member.Name == nameof(SourceWithNested.ChildToMap) &&
                srcMemberExpr.Member.Name == nameof(NestedSourceProp.NestedName))
            {
                // Construye la ruta de destino: d.MappedChild
                var mappedChildPropInfo = typeof(DestWithNested).GetProperty(nameof(DestWithNested.MappedChild));
                var destMappedChildAccess = Expression.Property(destParamExpr, mappedChildPropInfo!);

                // Construye la ruta final: d.MappedChild.InnerName
                var innerNamePropInfo = typeof(NestedDestPropDifferentName).GetProperty(nameof(NestedDestPropDifferentName.InnerName));
                return Expression.Property(destMappedChildAccess, innerNamePropInfo!);
            }
            return null;
        };
        
        var options = new ExpressionConverterOptions().WithCustomMap(customMap);

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceWithNested, DestWithNested>(sourcePredicate, options);
        
        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "DeepMap" } }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "Wrong" } }));
    }
}
