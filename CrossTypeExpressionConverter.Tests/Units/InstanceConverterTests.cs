using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Tests for the <see cref="ExpressionConverter"/> class.
/// </summary>
[TestFixture]
public class InstanceConverterTests
{
    [Test]
    public void Convert_UsesConfiguredMemberMap()
    {
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string>
            {
                { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) }
            });

        var instance = new ExpressionConverter(options);

        Expression<Func<SourceSimple, bool>> predicate = s => s.Name == "MapTest";
        var converted = instance.Convert<SourceSimple, DestDifferentNames>(predicate);

        Assert.That(TestUtils.Evaluate(converted, new DestDifferentNames { FullName = "MapTest" }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestDifferentNames { FullName = "Other" }), Is.False);
    }

    [Test]
    public void Convert_UsesErrorHandlingStrategy()
    {
        var options = new ExpressionConverterOptions()
            .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault)
            .WithMemberMap(new Dictionary<string, string>
            {
                { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) }
            });

        var instance = new ExpressionConverter(options);
        Expression<Func<SourceSimple, bool>> predicate = s => s.Name == "x" && s.PropertyToIgnoreOnDest == "y";
        var converted = instance.Convert<SourceSimple, DestDifferentNames>(predicate);

        Assert.That(TestUtils.Evaluate(converted, new DestDifferentNames { FullName = "x" }), Is.False);
    }

    [Test]
    public void Convert_UsesCustomMap()
    {
        Expression<Func<SourceForCustomMap, bool>> predicate = s => s.Data == "PREFIX_Original";
        Func<MemberExpression, ParameterExpression, Expression?> customMap = (member, param) =>
        {
            if (member.Member.Name == nameof(SourceForCustomMap.Data))
            {
                var prop = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.TransformedData));
                return prop != null ? Expression.Property(param, prop) : null;
            }
            return null;
        };

        var options = new ExpressionConverterOptions().WithCustomMap(customMap);
        var instance = new ExpressionConverter(options);
        var converted = instance.Convert<SourceForCustomMap, DestForCustomMap>(predicate);

        Assert.That(TestUtils.Evaluate(converted, new DestForCustomMap { TransformedData = "PREFIX_Original" }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestForCustomMap { TransformedData = "Original" }), Is.False);
    }
}
