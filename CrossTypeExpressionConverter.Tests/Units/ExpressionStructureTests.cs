using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para verificar la preservación de la estructura de la expresión,
/// como operadores lógicos y llamadas a métodos.
/// </summary>
[TestFixture]
public class ExpressionStructureTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    // --- Pruebas de Lógica Booleana ---
    [Test]
    public void Convert_AndLogic_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 5 && s.IsActive;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 3, IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = false }));
    }

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
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 1, FullName = "Other" }));
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Valid" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Other" }));
    }

    // --- Pruebas de Llamadas a Métodos ---
    [Test]
    public void Convert_StringStartsWith_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name != null && s.Name.StartsWith("Prefix");

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Name = "PrefixValue" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = "NoPrefix" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = null }));
    }

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
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "StartMiddleEnd" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "NoMatch" }));
    }
    
    // --- Prueba de Valores Constantes ---
    [Test]
    public void Convert_WithConstantComparison_ShouldPreserveConstantLogic()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 0 && 10 > 5; // 10 > 5 es siempre verdadero

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 1 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = -1 }));
    }
}
