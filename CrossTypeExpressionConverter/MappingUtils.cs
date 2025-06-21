using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

public static class MappingUtils
{
    /// <summary>
    /// Builds a dictionary mapping source property names to destination property names based
    /// on a lambda expression that uses an object initializer (e.g. <c>src =&gt; new Dest { DestProp = src.SrcProp }</c>).
    /// </summary>
    /// <param name="mapping">
    /// A lambda expression that maps a source type to a destination type using an object initializer (e.g., <c>src => new Dest { DestProp = src.SrcProp }</c>).
    /// </param>
    /// <returns>
    /// A dictionary where each key is a source property name and each value is the corresponding destination property name.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provided expression does not use an object initializer.
    /// </exception>
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