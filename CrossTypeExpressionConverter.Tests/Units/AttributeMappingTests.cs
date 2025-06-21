using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

[TestFixture]
public class AttributeMappingTests
{
    [Test]
    public void Convert_Property_WithMapsToAttribute_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceWithAttributes, bool>> predicate = s => s.Id == 5;

        var converted = ExpressionConverter.Convert<SourceWithAttributes, DestAttributeTarget>(predicate);

        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { IdAlias = 5 }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { IdAlias = 3 }), Is.False);
    }

    [Test]
    public void MemberMap_ShouldOverride_MapsToAttribute()
    {
        Expression<Func<SourceWithAttributes, bool>> predicate = s => s.Id == 10;
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceWithAttributes.Id), nameof(DestAttributeTarget.AltId) } });

        var converted = ExpressionConverter.Convert<SourceWithAttributes, DestAttributeTarget>(predicate, options);

        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { AltId = 10 }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { AltId = 5 }), Is.False);
    }

    [Test]
    public void CustomMap_ShouldOverride_MemberMap_And_Attribute()
    {
        Expression<Func<SourceWithAttributes, bool>> predicate = s => s.Id == 7;
        Func<MemberExpression, ParameterExpression, Expression?> customMap = (src, destParam) =>
        {
            if (src.Member.Name == nameof(SourceWithAttributes.Id))
            {
                var altProp = typeof(DestAttributeTarget).GetProperty(nameof(DestAttributeTarget.AltId));
                return Expression.Property(destParam, altProp!);
            }
            return null;
        };
        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceWithAttributes.Id), nameof(DestAttributeTarget.IdAlias) } })
            .WithCustomMap(customMap);

        var converted = ExpressionConverter.Convert<SourceWithAttributes, DestAttributeTarget>(predicate, options);

        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { AltId = 7 }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { AltId = 3 }), Is.False);
    }

    [Test]
    public void Convert_FallsBack_ToNameMatching_WhenNoMappingSpecified()
    {
        Expression<Func<SourceWithAttributes, bool>> predicate = s => s.IsActive;

        var converted = ExpressionConverter.Convert<SourceWithAttributes, DestAttributeTarget>(predicate);

        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { IsActive = true }), Is.True);
        Assert.That(TestUtils.Evaluate(converted, new DestAttributeTarget { IsActive = false }), Is.False);
    }
}

