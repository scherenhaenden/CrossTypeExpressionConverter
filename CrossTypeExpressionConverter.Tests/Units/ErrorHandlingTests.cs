using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contiene tests para el manejo de errores durante el mapeo de miembros.
/// </summary>
[TestFixture]
public class ErrorHandlingTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    [Test]
    public void Convert_MemberMissingOnDestination_ShouldThrow_ByDefault()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "Test";
        // Por defecto, ErrorHandling es 'Throw'
        var options = ExpressionConverterOptions.Default; 

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options)
        );
        Assert.That(ex.Message, Does.Contain(nameof(SourceSimple.PropertyToIgnoreOnDest)));
    }
    
    [Test]
    public void Convert_MemberInMapMissingOnDestination_ShouldThrow_ByDefault()
    {
        // Arrange
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1;
        var options = ExpressionConverterOptions.Default
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceSimple.Id), "NonExistentDestProperty" } });

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options)
        );
        Assert.That(ex.Message, Does.Contain("NonExistentDestProperty"));
    }

    [Test]
    public void Convert_MemberMissingOnDestination_WithReturnDefault_ShouldEvaluateToFalse()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault);
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "Test";

        // Act
        var converted = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options);

        // Assert: El predicado se convierte en (null == "Test"), que es falso.
        Assert.IsFalse(Evaluate(converted, new DestSimple()));
    }

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
        
        // Assert: El predicado se convierte en (0 == 1), que es falso.
        Assert.IsFalse(Evaluate(converted, new DestSimple { Id = 99 }));
    }

    [Test]
    public void Convert_CompoundExpressionWithMissingMember_WithReturnDefault_ShouldEvaluateCorrectly()
    {
        // Arrange
        var options = ExpressionConverterOptions.Default
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault);
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "A" || s.IsActive;

        // Act
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, options);

        // Assert: El predicado se convierte en (null == "A" || d.IsActive), que es (false || d.IsActive).
        // Debería ser verdadero si IsActive es verdadero.
        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { IsActive = false }));
    }
}
