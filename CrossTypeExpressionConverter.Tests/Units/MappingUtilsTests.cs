using System.Linq.Expressions;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Tests for <see cref="MappingUtils.BuildMemberMap{TSource,TDestination}"/>.
/// </summary>
[TestFixture]
public class MappingUtilsTests
{
    // ─────────────────────────────────────────────────────────────────────
    //  ✔ Dummy types just for these tests
    // ─────────────────────────────────────────────────────────────────────
    private class Source
    {
        public int    Id   { get; set; }
        public string? Name { get; set; }
    }

    private class Dest
    {
        public int    EntityId { get; set; }
        public string? FullName { get; set; }
    }

    //  Happy-path: direct one-to-one bindings
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Tests that a simple one-to-one mapping returns the correct dictionary.
    /// </summary>
    [Test]
    public void BuildMemberMap_SimpleMapping_ReturnsCorrectDictionary()
    {
        // src => new Dest { EntityId = src.Id, FullName = src.Name }
        Expression<Func<Source, Dest>> mapping =
            src => new Dest
            {
                EntityId = src.Id,
                FullName = src.Name
            };

        var dict = MappingUtils.BuildMemberMap(mapping);

        Assert.That(dict, Has.Count.EqualTo(2));
        Assert.That(dict["Id"],   Is.EqualTo("EntityId"));
        Assert.That(dict["Name"], Is.EqualTo("FullName"));
    }

    //  Guard-clause: body must be a MemberInitExpression
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Tests that building a member map with a non-MemberInitExpression throws an InvalidOperationException.
    /// </summary>
    [Test]
    public void BuildMemberMap_NonMemberInit_ThrowsInvalidOperationException()
    {
        // No object-initializer => NewExpression, not MemberInitExpression
        Expression<Func<Source, Dest>> mapping = src => new Dest();

        Assert.Throws<InvalidOperationException>(
            () => MappingUtils.BuildMemberMap(mapping));
    }

    //  Only *direct* member accesses should be mapped
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Tests that non-direct member assignments are ignored in member mapping.
    /// </summary>
    [Test]
    public void BuildMemberMap_IgnoresNonDirectMemberAssignments()
    {
        // FullName uses the null-coalescing operator – not a plain MemberExpression
        Expression<Func<Source, Dest>> mapping =
            src => new Dest
            {
                EntityId = src.Id,
                FullName = src.Name ?? "N/A"
            };

        var dict = MappingUtils.BuildMemberMap(mapping);

        Assert.That(dict, Has.Count.EqualTo(1));
        Assert.That(dict.ContainsKey("Id"),   Is.True);
        Assert.That(dict["Id"], Is.EqualTo("EntityId"));
        Assert.That(dict.ContainsKey("Name"), Is.False,
            "Bindings that are not simple member-accesses should be skipped.");
    }
}