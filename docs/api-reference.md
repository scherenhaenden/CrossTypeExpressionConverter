# API Reference

This document provides a formal API reference for the public members of `CrossTypeExpressionConverter`.

> **Namespace:** `CrossTypeExpressionConverter`  
> **Assembly:** `CrossTypeExpressionConverter.dll`  
> **NuGet Package:** [`CrossTypeExpressionConverter`](https://www.nuget.org/packages/CrossTypeExpressionConverter/)

---

## `IExpressionConverter` (Interface)
Defines the contract for expression converters. Register this interface in your DI container to decouple expression mapping from your core services.

### Methods
#### `Convert<TSource, TDestination>`
Converts a predicate expression from a source type to a destination type.
```csharp
Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
    Expression<Func<TSource, bool>> sourcePredicate);
```
- **Type Parameters**:
  - `TSource`: The type of the parameter in the source expression.
  - `TDestination`: The type of the parameter in the returned expression.
- **Parameters**:
  - `sourcePredicate`: The input filter expression to translate.
- **Returns**: A new, translated expression tree targeting `TDestination`.

---

## `ExpressionConverter` (Class)
The primary instance-based implementation of `IExpressionConverter`. Thread-safe and designed for registration as a Singleton.

> **Thread Safety:** This class is fully thread-safe. It holds only a readonly reference to `ExpressionConverterOptions`, and the internal `ExpressionVisitor` is created fresh for each `Convert` call. The static attribute cache uses `MemoryCache`, which is thread-safe.

### Constructor
```csharp
public ExpressionConverter(ExpressionConverterOptions options);
```
- **Parameters**:
  - `options`: Configuration options control conversion behavior.
- **Exceptions**: Throws `ArgumentNullException` if `options` is null.

### Methods
#### `Convert<TSource, TDestination>`
Converts the predicate according to the instance's configured options.
```csharp
public Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
    Expression<Func<TSource, bool>> sourcePredicate);
```

---

## `ExpressionConverterFacade` (Static Class)
Provides a static entry point for converting expressions. Useful for quick scripting, legacy compatibility, or cases where Dependency Injection is not available.

### Methods
#### `Convert<TSource, TDestination>` (With Options)
Converts an expression using the specified configuration options.
```csharp
public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
    Expression<Func<TSource, bool>> sourcePredicate,
    ExpressionConverterOptions options);
```

#### `Convert<TSource, TDestination>` (With Explicit Maps)
Converts an expression using inline mapping parameters (implicitly creates a temporary options object).
```csharp
public static Expression<Func<TDestination, bool>> Convert<TSource, TDestination>(
    Expression<Func<TSource, bool>> sourcePredicate,
    IDictionary<string, string>? memberMap = null,
    Func<MemberExpression, ParameterExpression, Expression?>? customMap = null);
```

---

## `ExpressionConverterOptions` (Class)
Contains settings that alter the behavior of the converter. Implements a fluent, immutable API.

> **Immutability:** All `With*()` methods return a **new instance**. The original options object is never modified. This pattern ensures that options can be safely shared across threads without synchronization.

### Properties
- **`ErrorHandling`** (`MemberMappingErrorHandling`): Determines how the converter reacts to unmappable properties. Default is `Throw`.
- **`MemberMap`** (`IDictionary<string, string>?`): Map dictionary where keys are source properties and values are destination properties.
- **`CustomMap`** (`Func<MemberExpression, ParameterExpression, Expression?>?`): Delegate providing custom replacement expression trees for visited properties.

### Static Properties
- **`Default`** (`ExpressionConverterOptions`): Returns a new options instance initialized with default settings.

### Methods
#### `WithErrorHandling`
```csharp
public ExpressionConverterOptions WithErrorHandling(MemberMappingErrorHandling errorHandling);
```
- Returns a **new** options instance with the specified error-handling strategy.

#### `WithMemberMap`
```csharp
public ExpressionConverterOptions WithMemberMap(IDictionary<string, string>? memberMap);
```
- Returns a **new** options instance with the specified member map dictionary.

#### `WithCustomMap`
```csharp
public ExpressionConverterOptions WithCustomMap(Func<MemberExpression, ParameterExpression, Expression?>? customMap);
```
- Returns a **new** options instance with the specified custom mapping delegate.

---

## `MappingUtils` (Static Class)
Provides helper utilities for configuring mapping dictionaries.

### Methods
#### `BuildMemberMap<TSource, TDestination>`
Builds a property map dictionary from an object initializer expression.
```csharp
public static IDictionary<string, string> BuildMemberMap<TSource, TDestination>(
    Expression<Func<TSource, TDestination>> mapping);
```
- **Parameters**:
  - `mapping`: A lambda expression returning `TDestination` populated via an object initializer (e.g. `src => new Dest { D1 = src.S1 }`).
- **Returns**: An `IDictionary<string, string>` where keys are source property names and values are destination property names.
- **Exceptions**: Throws `InvalidOperationException` if the expression body is not a `MemberInitExpression`.

---

## `MapsToAttribute` (Attribute)
Decorates properties on source models to declare their equivalent target property names on destination models.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MapsToAttribute : Attribute
```

### Constructor
```csharp
public MapsToAttribute(string destinationMemberName);
```

### Properties
- **`DestinationMemberName`** (`string`): The name of the property on the destination type that this property maps to.

---

## `MemberMappingErrorHandling` (Enum)
Specifies how the converter handles missing or unresolvable properties.

```csharp
public enum MemberMappingErrorHandling
```

### Fields
- **`Throw`** (`0`): Throws an `InvalidOperationException` describing the missing mapping. Safe default.
- **`ReturnDefault`** (`1`): Ignores the missing property and generates an expression returning `default(PropertyType)`. Warning: Can lead to silent query logic shifts.

---

<p align="center">
  <a href="README.md">📚 Documentation Index</a>
</p>
