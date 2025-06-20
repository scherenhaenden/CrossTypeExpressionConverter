using System;
using System.Linq.Expressions;

namespace CrossTypeExpressionConverter;

/// <summary>
/// Proporciona un conjunto de opciones para configurar el comportamiento de <see cref="ExpressionConverter"/>.
/// Esta clase utiliza una interfaz fluida para promover la inmutabilidad.
/// </summary>
public sealed class ExpressionConverterOptions
{
    /// <summary>
    /// Obtiene una instancia por defecto de las opciones de configuración.
    /// </summary>
    public static ExpressionConverterOptions Default => new();

    /// <summary>
    /// Define cómo se manejan los errores de mapeo de miembros. Por defecto es 'Throw'.
    /// </summary>
    public MemberMappingErrorHandling ErrorHandling { get; init; }

    /// <summary>
    /// Un diccionario que mapea nombres de miembros del tipo de origen a los del tipo de destino.
    /// </summary>
    public IDictionary<string, string>? MemberMap { get; init; }

    /// <summary>
    /// Un callback para proporcionar expresiones de reemplazo personalizadas para miembros específicos.
    /// </summary>
    public Func<MemberExpression, ParameterExpression, Expression?>? CustomMap { get; init; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExpressionConverterOptions"/> con valores por defecto.
    /// </summary>
    public ExpressionConverterOptions()
    {
        ErrorHandling = MemberMappingErrorHandling.Throw;
        MemberMap = null;
        CustomMap = null;
    }

    /// <summary>
    /// Constructor privado para uso interno de la interfaz fluida.
    /// </summary>
    private ExpressionConverterOptions(
        MemberMappingErrorHandling errorHandling,
        IDictionary<string, string>? memberMap,
        Func<MemberExpression, ParameterExpression, Expression?>? customMap)
    {
        ErrorHandling = errorHandling;
        MemberMap = memberMap;
        CustomMap = customMap;
    }

    /// <summary>
    /// Crea una nueva instancia de opciones con el manejo de errores especificado.
    /// </summary>
    public ExpressionConverterOptions WithErrorHandling(MemberMappingErrorHandling errorHandling)
    {
        return new ExpressionConverterOptions(errorHandling, MemberMap, CustomMap);
    }

    /// <summary>
    /// Crea una nueva instancia de opciones con el mapa de miembros especificado.
    /// </summary>
    public ExpressionConverterOptions WithMemberMap(IDictionary<string, string>? memberMap)
    {
        return new ExpressionConverterOptions(ErrorHandling, memberMap, CustomMap);
    }

    /// <summary>
    /// Crea una nueva instancia de opciones con el mapeo personalizado especificado.
    /// </summary>
    public ExpressionConverterOptions WithCustomMap(Func<MemberExpression, ParameterExpression, Expression?>? customMap)
    {
        return new ExpressionConverterOptions(ErrorHandling, MemberMap, customMap);
    }
}