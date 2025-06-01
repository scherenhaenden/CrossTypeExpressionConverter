# CrossTypeExpressionConverter

[![NuGet Version](https://img.shields.io/nuget/v/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/scherenhaenden/CrossTypeExpressionConverter/dotnet.yml?branch=main&style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/actions)
[![License](https://img.shields.io/github/license/scherenhaenden/CrossTypeExpressionConverter.svg?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/blob/main/LICENSE)

**CrossTypeExpressionConverter** is a .NET library designed to seamlessly translate LINQ predicate expressions (`Expression<Func<TSource, bool>>`) from a source type (`TSource`) to an equivalent expression for a destination type (`TDestination`). This is particularly powerful when working with different layers in your application, such as mapping query logic between domain entities and Data Transfer Objects (DTOs), while ensuring full compatibility with `IQueryable` providers like Entity Framework Core for efficient server-side query execution.

Stop rewriting similar filter logic for different types! `CrossTypeExpressionConverter` allows you to define your logic once and reuse it across various type representations.

---

## 🌟 Key Features

* **Type-Safe Conversion**: Translates strongly-typed LINQ expressions, reducing the risk of runtime errors.
* **`IQueryable` Compatible**: Generated expressions are fully translatable by LINQ providers (e.g., Entity Framework Core), ensuring filters are applied at the database level for optimal performance.
* **Flexible Member Mapping**:
    * **Automatic Name Matching**: By default, matches properties with the same name.
    * **Explicit Dictionary Mapping**: Provide an `IDictionary<string, string>` to map properties with different names.
    * **Custom Delegate Mapping**: Supply a `Func<MemberExpression, ParameterExpression, Expression?>` for complex, custom translation logic for specific members.
* **Nested Property Support**: Correctly handles expressions involving nested properties (e.g., `customer => customer.Address.Street == "Main St"`) by translating member access chains.
* **Reduced Boilerplate**: Eliminates the need to manually reconstruct expression trees or write repetitive mapping logic.

---

## 💾 Installation

You can install `CrossTypeExpressionConverter` via NuGet Package Manager:

```shell
Install-Package CrossTypeExpressionConverter -Version 0.1.0
````

Or using the .NET CLI:

```shell
dotnet add package CrossTypeExpressionConverter --version 0.1.0
```

*(Once published, you can remove `-Version 0.1.0` to get the latest stable version.)*

-----

## 🚀 Quick Start: Basic Usage

Let's say you have a `User` domain model and a `UserEntity` for your database context, and some property names differ.

**1. Define Your Types:**

```csharp
// Domain Model
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime BirthDate { get; set; }
}

// Database Entity
public class UserEntity
{
    public int UserId { get; set; } // Different name for ID
    public string? UserName { get; set; } // Different name for Name
    public bool Enabled { get; set; }    // Different name for IsActive
    public DateTime DateOfBirth { get; set; } // Different name for BirthDate
}
```

**2. Define a Reusable Domain Filter:**

```csharp
using System.Linq.Expressions;
using CrossTypeExpressionConverter; // Your package's namespace

// Filter for active adult users
Expression<Func<User, bool>> isActiveAdultUserDomainFilter =
    user => user.IsActive && user.BirthDate <= DateTime.Today.AddYears(-18);
```

**3. Configure Mapping (if names differ):**

You can provide an `IDictionary<string, string>` to map differing property names.

```csharp
var userToEntityMap = new Dictionary<string, string>
{
    { nameof(User.Id), nameof(UserEntity.UserId) }, // Map User.Id to UserEntity.UserId
    { nameof(User.Name), nameof(UserEntity.UserName) },
    { nameof(User.IsActive), nameof(UserEntity.Enabled) },
    { nameof(User.BirthDate), nameof(UserEntity.DateOfBirth) }
};
```

*(You might create a helper utility, like a `MappingUtils.BuildMemberMap` method, in your own project to generate this dictionary from a LINQ initializer expression for better refactoring safety, but such a utility is not part of this package in v0.1.0.)*

**4. Convert the Expression:**

```csharp
// Convert using the memberMap
Expression<Func<UserEntity, bool>> entityPredicate =
    ExpressionConverter.Convert<User, UserEntity>(isActiveAdultUserDomainFilter, userToEntityMap);

// If all property names matched, you could omit the memberMap:
// Expression<Func<User, bool>> idFilter = u => u.Id == 1;
// Expression<Func<UserEntity, bool>> entityIdFilter =
//     ExpressionConverter.Convert<User, UserEntity>(idFilter, new Dictionary<string,string> { { "Id", "UserId" } });
```

**5. Use it in Your `IQueryable` Query (e.g., EF Core):**

```csharp
// Assuming 'dbContext' is your EF Core DbContext instance
// IQueryable<UserEntity> usersQuery = dbContext.Set<UserEntity>();

// var adultEntities = usersQuery.Where(entityPredicate).ToList();

// foreach (var entity in adultEntities)
// {
//     Console.WriteLine($"Found adult user: {entity.UserName}");
// }
```

The `entityPredicate` will be translated by Entity Framework Core into the appropriate SQL `WHERE` clause.

-----

## 🛠️ Advanced Usage: `memberMap` and `customMap`

The `ExpressionConverter.Convert` method takes two optional parameters for controlling the mapping:

  * **`IDictionary<string, string>? memberMap = null`**:

      * This dictionary allows you to explicitly define mappings between source member names (keys) and destination member names (values).
      * If a source member name is found in this dictionary, its value will be used as the target member name on the destination type.
      * This takes precedence over automatic name matching.
      * If a member is not in the map, automatic name matching is attempted.
      * Example: `new Dictionary<string, string> { { "SourcePropertyName", "DestinationPropertyName" } }`

  * **`Func<MemberExpression, ParameterExpression, Expression?>? customMap = null`**:

      * This delegate provides the ultimate control for complex mapping scenarios where direct name or dictionary mapping is insufficient.
      * The function is called for each member access on the `TSource` type.
      * **Parameters:**
          * `sourceMemberExpr`: The original `MemberExpression` from the source predicate (e.g., `user.IsActive`).
          * `destParamExpr`: The `ParameterExpression` for the destination type (e.g., `entity` of type `UserEntity`).
      * **Return Value:**
          * If you return a non-null `Expression`, that expression will be used directly as the replacement in the new expression tree. This allows you to, for example, call a method on the destination, combine multiple destination properties, or apply transformations.
          * If you return `null`, the converter will fall back to using `memberMap` (if provided and applicable) or then automatic name matching for that specific member.
      * This `customMap` has the highest precedence in the mapping logic.

### Custom Mapping Example:

Map `SourceType.Data` to `DestinationType.ProcessedData` but prefix the original comparison value.
(Note: `customMap` replaces the member access part, not the entire comparison. The original comparison operator and constant will be applied to the expression returned by `customMap`.)

```csharp
public class SourceType { public string Data { get; set; } }
public class DestinationType { public string ProcessedData { get; set; } }

Expression<Func<SourceType, bool>> sourceFilter = s => s.Data == "value";

Func<MemberExpression, ParameterExpression, Expression?> myCustomMap = (srcMember, destParam) =>
{
    if (srcMember.Member.Name == nameof(SourceType.Data))
    {
        // Replace 's.Data' with 'd.ProcessedData'
        PropertyInfo destProp = typeof(DestinationType).GetProperty(nameof(DestinationType.ProcessedData));
        return Expression.Property(destParam, destProp);
    }
    return null; // Fallback for other members
};

Expression<Func<DestinationType, bool>> destFilter =
    ExpressionConverter.Convert<SourceType, DestinationType>(sourceFilter, customMap: myCustomMap);

// The destFilter will be equivalent to: d => d.ProcessedData == "value"

// A more complex customMap might transform the value itself or map to a method, e.g.:
// Expression<Func<SourceType, bool>> complexFilter = s => s.NumericValue > 10;
// Func<MemberExpression, ParameterExpression, Expression?> complexCustomMap = (srcMember, destParam) =>
// {
//     if (srcMember.Member.Name == "NumericValue")
//     {
//         // Map s.NumericValue to d.CalculationResult (which might be int)
//         PropertyInfo destProp = typeof(DestinationTypeWithCalc).GetProperty("CalculationResult");
//         return Expression.Property(destParam, destProp);
//     }
//     return null;
// };
// This would convert complexFilter to d => d.CalculationResult > 10
```

Crafting correct expressions within `customMap` requires a good understanding of `System.Linq.Expressions`.

-----

## 💡 How It Works

`CrossTypeExpressionConverter` walks the provided LINQ expression tree using a custom `ExpressionVisitor`:

1.  **Parameter Replacement**: The `ParameterExpression` of `TSource` (e.g., `user` in `user => user.IsActive`) is replaced with a new `ParameterExpression` of `TDestination`. The name of the source parameter is preserved if possible.
2.  **Member Access Translation**: For each `MemberExpression` (e.g., `user.IsActive`) that declares `TSource` as its type:
      * It first checks if the `customMap` delegate provides a specific translation.
      * If not, it consults the `memberMap` dictionary.
      * If not found there, it attempts to find a member on `TDestination` with the same name.
      * If no corresponding member is found on `TDestination` by any means, an `InvalidOperationException` is thrown.
      * Crucially, it recursively visits the expression *on which the member is accessed* (e.g., `user.Address` in `user.Address.Street`). This ensures that chains of property accesses are correctly reconstructed on the destination type, maintaining `IQueryable` translatability.
3.  **New Lambda Creation**: A new `LambdaExpression` is constructed using the translated body and the new `TDestination` parameter.

This process retargets the original expression's logic to operate on `TDestination` properties.

-----

## 🛣️ Roadmap & Future Enhancements (v0.2.0 and beyond)

While `CrossTypeExpressionConverter v0.1.0` focuses on robust predicate conversion, future versions may include:

  * **`ExpressionConverterOptions` Object**: Introduce a dedicated options class to simplify the `Convert` method signature and allow for more configuration points (e.g., `ThrowOnFailedMemberMapping` toggle, case-sensitivity options for name matching).
  * **Selector Conversion**: Support for converting projection expressions (e.g., `Expression<Func<TSource, TResult>>` to `Expression<Func<TDestination, TDestResult>>`).
  * **Order By Conversion**: Support for key selector expressions for ordering.
  * **Fluent Configuration API**: A fluent interface for defining mappings.
  * **Performance Caching**: Internal caching of `MemberInfo` and type mapping details.
  * **Attribute-Based Mapping**: Define mappings via attributes on type members.
  * **Roslyn Analyzer**: For compile-time diagnostics of potential mapping issues.
  * **`MappingUtils` Integration**: Potentially include a utility like `MappingUtils.BuildMemberMap` or an overload that accepts a mapping expression.

-----

## 🤝 Contributing

Contributions are welcome\! If you have an idea for a new feature, an improvement, or a bug fix, please:

1.  Check the [Issues](https://www.google.com/search?q=https://github.com/scherenhaenden/CrossTypeExpressionConverter/issues) to see if your idea or bug has already been discussed.
2.  If not, open a new issue to discuss the change.
3.  Fork the repository, make your changes in a feature branch, and submit a pull request with a clear description of your changes.

Please ensure that any new code includes appropriate unit tests.

-----

## ❓ FAQ

  * **Q: Does this work with complex nested objects?**
      * A: Yes, for member access chains like `source.Order.Details.ProductName`. The converter will attempt to map each segment. If `source.Order` maps to `dest.CustomerOrder`, it will then try to resolve `Details` on `CustomerOrder`'s type, and so on. Complex re-structuring of nested paths (e.g., flattening `source.Order.Details.ProductName` to `dest.ProductName`) would typically require `customMap`.
  * **Q: What happens if a property doesn't exist on the destination type and isn't mapped?**
      * A: The converter will throw an `InvalidOperationException` detailing which member could not be mapped. In v0.1.0, this behavior is not configurable.
  * **Q: Is this similar to AutoMapper's `ProjectTo`?**
      * A: It shares the goal of translating expressions for ORM querying. However, `CrossTypeExpressionConverter` is a more focused utility for converting individual `Expression<Func<TSource, bool>>`. It doesn't perform full object-to-object mapping or offer `IQueryable` extension methods like `ProjectTo` out-of-the-box, but it can be a powerful component in building such systems or for more direct expression manipulation.

-----

## ⚖️ License

This project is licensed under the **MIT License**. See the [LICENSE](https://www.google.com/url?sa=E&source=gmail&q=https://github.com/scherenhaenden/CrossTypeExpressionConverter/blob/main/LICENSE) file for details.

-----

Copyright (c) 2025 scherenhaenden