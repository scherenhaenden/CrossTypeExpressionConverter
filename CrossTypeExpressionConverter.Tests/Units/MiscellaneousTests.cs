using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para funcionalidades misceláneas del convertidor.
/// </summary>
[TestFixture]
public class MiscellaneousTests
{
    [Test]
    public void Convert_ShouldPreserveSourceParameterName_IfExists()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = item => item.Id == 1; // El nombre del parámetro es "item"

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);
        
        // Assert
        Assert.AreEqual("item", convertedPredicate.Parameters[0].Name);
    }

    [Test]
    public void Convert_ShouldUseDefaultParameterName_IfSourceHasNone()
    {
        // Arrange
        // Se crea una expresión manualmente sin un nombre de parámetro explícito.
        var param = Expression.Parameter(typeof(SourceSimple)); 
        var body = Expression.Equal(Expression.Property(param, nameof(SourceSimple.Id)), Expression.Constant(1));
        var sourcePredicate = Expression.Lambda<Func<SourceSimple, bool>>(body, param);

        // El conversor usará el nombre generado por el compilador si existe, o "p" como último recurso.
        var expectedName = param.Name ?? "p";

        // Act
        var converted = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);
        
        // Assert
        Assert.AreEqual(expectedName, converted.Parameters[0].Name);
    }
}