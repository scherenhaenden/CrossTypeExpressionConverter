using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para la funcionalidad de mapeo personalizado usando 'customMap'.
/// </summary>
[TestFixture]
public class CustomMappingTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

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
            return null; // Fallback para otros miembros
        };

        var options = new ExpressionConverterOptions().WithCustomMap(customMap);

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, options);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "PREFIX_Original" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "Original" }));
    }
    
    [Test]
    public void Convert_NumericProperty_WithCustomMap_ToTransformedValue()
    {
        // Arrange
        // El predicado original comprueba si NumericValue es 10.
        Expression<Func<SourceForCustomMap, bool>> sourcePredicate = s => s.NumericValue == 10;

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (sourceMemberExpr, destParamExpr) =>
        {
            if (sourceMemberExpr.Member.Name == nameof(SourceForCustomMap.NumericValue))
            {
                // Mapeamos s.NumericValue a una expresión que representa 'd.DoubledValue / 2'.
                // Así, la condición 'd.DoubledValue / 2 == 10' se convierte en 'd.DoubledValue == 20'.
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
        var convertedPredicate = ExpressionConverter.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, options);
        
        // Assert
        // El predicado convertido debe ser verdadero para un objeto destino donde DoubledValue es 20.
        Assert.IsTrue(Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 20 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 10 }));
    }
}
