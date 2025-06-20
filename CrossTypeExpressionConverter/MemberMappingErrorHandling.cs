namespace CrossTypeExpressionConverter;

/// <summary>
/// Especifica cómo reacciona el convertidor cuando un miembro no se puede mapear.
/// </summary>
public enum MemberMappingErrorHandling
{
    /// <summary>
    /// Lanza una InvalidOperationException (comportamiento por defecto).
    /// Use esta opción para fallar rápido y asegurar que todos los miembros estén correctamente mapeados.
    /// </summary>
    Throw,

    /// <summary>
    /// Ignora el miembro faltante y sustituye una expresión que devuelve el valor por defecto del tipo del miembro (p. ej., null para objetos, 0 para números).
    /// ¡Atención! Esto puede causar que los predicados se evalúen de forma inesperada (p. ej., una condición puede volverse falsa silenciosamente).
    /// Es útil para mapear tipos que solo coinciden parcialmente sin que la aplicación se detenga.
    /// </summary>
    ReturnDefault
}