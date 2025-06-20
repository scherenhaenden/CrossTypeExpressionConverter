using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para la funcionalidad de mapeo básica.
/// </summary>
[TestFixture]
public class BasicMappingTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    // --- Pruebas de Mapeo por Coincidencia de Nombre ---
    [Test]
    public void Convert_SimpleIntProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 10;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 10 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 5 }));
    }

    [Test]
    public void Convert_SimpleStringProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "Test";

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Name = "Test" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = "Other" }));
    }

    [Test]
    public void Convert_SimpleBoolProperty_SameName_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.IsActive; // Equivalente a s.IsActive == true

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { IsActive = false }));
    }

    // --- Pruebas de Mapeo Basado en Diccionario (MemberMap) ---
    [Test]
    public void Convert_IntProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 20;
        var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Id), nameof(DestDifferentNames.EntityId) } };

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 20 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 15 }));
    }

    [Test]
    public void Convert_StringProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "MapTest";
        var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) } };

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        // Assert
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "MapTest" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "OtherMap" }));
    }
}
