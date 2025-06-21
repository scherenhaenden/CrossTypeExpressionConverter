using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

[TestFixture]
public class AttributeMappingTests
{
    [Test]
    public void Convert_WithAttributeMapping_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceWithAttributes, bool>> predicate = s => s.Id == 5 && s.IsActive;

        var converted = ExpressionConverterFacade.Convert<SourceWithAttributes, DestDifferentNames>(predicate);

        var dest = new DestDifferentNames { EntityId = 5, Enabled = true };
        Assert.That(TestUtils.Evaluate(converted, dest), Is.True);
    }

    [Test]
    public void MemberMap_Overrides_AttributeMapping()
    {
        Expression<Func<SourceWithAttributeId, bool>> predicate = s => s.Id == 5;

        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceWithAttributeId.Id), nameof(DestForCustomMap.DoubledValue) } });

        var converted = ExpressionConverterFacade.Convert<SourceWithAttributeId, DestForCustomMap>(predicate, options);

        var dest = new DestForCustomMap { DoubledValue = 5 };
        Assert.That(TestUtils.Evaluate(converted, dest), Is.True);
    }

    [Test]
    public void CustomMap_Overrides_MemberMap_And_Attribute()
    {
        Expression<Func<SourceWithAttributeId, bool>> predicate = s => s.Id == 5;

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (member, param) =>
        {
            if (member.Member.Name == nameof(SourceWithAttributeId.Id))
            {
                var prop = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.Id));
                return prop != null ? Expression.Property(param, prop) : null;
            }
            return null;
        };

        var options = new ExpressionConverterOptions()
            .WithMemberMap(new Dictionary<string, string> { { nameof(SourceWithAttributeId.Id), nameof(DestForCustomMap.DoubledValue) } })
            .WithCustomMap(customMap);

        var converted = ExpressionConverterFacade.Convert<SourceWithAttributeId, DestForCustomMap>(predicate, options);

        var dest = new DestForCustomMap { Id = 5 };
        Assert.That(TestUtils.Evaluate(converted, dest), Is.True);
    }

    [Test]
    public void FallsBack_To_ByName_When_NoAttributeOrMap()
    {
        Expression<Func<SourceMixedAttributes, bool>> predicate = s => s.Name == "Test";

        var converted = ExpressionConverterFacade.Convert<SourceMixedAttributes, DestSimple>(predicate);

        var dest = new DestSimple { Name = "Test" };
        Assert.That(TestUtils.Evaluate(converted, dest), Is.True);
    }
}
