using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

public static class MappingUtils
{
    /// <summary>
    /// Builds a name‑to‑name dictionary from a simple object‑initializer mapping
    /// (  src => new Dest { DestProp = src.SrcProp, … }  ).
    /// The resulting map has    SrcProp -> DestProp
    /// which is exactly what <see cref="ExpressionConverter"/> expects.
    /// </summary>
    public static IDictionary<string,string> BuildMemberMap<TSource,TDestination>(
        Expression<Func<TSource,TDestination>> mapping)
    {
        if (mapping.Body is not MemberInitExpression init)
            throw new InvalidOperationException("Mapping must use an object‑initializer.");

        var dict = new Dictionary<string,string>();

        foreach (var bind in init.Bindings.OfType<MemberAssignment>())
        {
            // Right side must be a direct member access on TSource
            if (bind.Expression is MemberExpression src &&
                src.Expression is ParameterExpression &&
                src.Member.DeclaringType == typeof(TSource))
            {
                dict[src.Member.Name] = bind.Member.Name;   // src -> dest
            }
        }
        return dict;
    }
}