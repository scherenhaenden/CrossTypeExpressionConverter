# CrossTypeExpressionConverter

[![NuGet Version](https://img.shields.io/nuget/v/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)

[![NuGet Downloads](https://img.shields.io/nuget/dt/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)

[![Build Status](https://img.shields.io/github/actions/workflow/status/scherenhaenden/CrossTypeExpressionConverter/dotnet.yml?branch=main&style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/actions)

[![License](https://img.shields.io/github/license/scherenhaenden/CrossTypeExpressionConverter.svg?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/blob/main/LICENSE)

[![NuGet Version (pre-release)](https://img.shields.io/nuget/vpre/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/scherenhaenden/CrossTypeExpressionConverter?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/releases/latest)

[![GitHub Release Downloads](https://img.shields.io/github/downloads/scherenhaenden/CrossTypeExpressionConverter/total?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/releases)

[![Build Status](https://img.shields.io/github/actions/workflow/status/scherenhaenden/CrossTypeExpressionConverter/dotnet.yml?branch=main\&style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/actions)

![Developed with Rider](https://img.shields.io/badge/Developed%20with-Rider-14345E?style=flat-square\&logo=jetbrains)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)

![C#](https://img.shields.io/badge/C%23-11-239120?style=flat-square\&logo=c-sharp\&logoColor=white)

![macOS](https://img.shields.io/badge/macOS-000000?style=flat-square\&logo=apple)
![Linux](https://img.shields.io/badge/Linux-FCC624?style=flat-square\&logo=linux\&logoColor=black)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=flat-square\&logo=windows\&logoColor=white)


**CrossTypeExpressionConverter** is a .NET library designed to seamlessly translate LINQ predicate expressions (`Expression<Func<TSource, bool>>`) from a source type (`TSource`) to an equivalent expression for a destination type (`TDestination`). This is particularly powerful when working with different layers in your application, such as mapping query logic between domain entities and Data Transfer Objects (DTOs), while ensuring full compatibility with `IQueryable` providers like Entity Framework Core for efficient server-side query execution.

Stop rewriting similar filter logic for different types! `CrossTypeExpressionConverter` allows you to define your logic once and reuse it across various type representations.

---

## 🌟 Key Features

* **Type-Safe Conversion**: Translates strongly-typed LINQ expressions, reducing the risk of runtime errors.
* **`IQueryable` Compatible**: Generated expressions are fully translatable by LINQ providers (e.g., Entity Framework Core), ensuring filters are applied at the database level for optimal performance.
* **Flexible Member Mapping**:
  * **Automatic Name Matching**: By default, matches properties with the same name.
  * **Mapping Utility**: Includes `MappingUtils.BuildMemberMap` to conveniently generate the member map dictionary from a type-safe LINQ projection expression.
  * **Explicit Dictionary Mapping**: Provide an `IDictionary<string, string>` to map properties with different names.
  * **Custom Delegate Mapping**: Supply a `Func<MemberExpression, ParameterExpression, Expression?>` for complex, custom translation logic for specific members.
* **Nested Property Support**: Correctly handles expressions involving nested properties (e.g., `customer => customer.Address.Street == "Main St"`) by translating member access chains.
* **Captured Variable Support**: Correctly processes predicates that compare against properties of captured (closed-over) variables (e.g., `s => s.Id == localOrder.Id`).
* **Reduced Boilerplate**: Eliminates the need to manually reconstruct expression trees or write repetitive mapping logic.

---

## 💾 Installation

You can install `CrossTypeExpressionConverter` via NuGet Package Manager:

```shell
Install-Package CrossTypeExpressionConverter -Version 0.2.1
```

Or using the .NET CLI:

```shell
dotnet add package CrossTypeExpressionConverter --version 0.2.1
```

---

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
    public bool Enabled { get; set; } // Different name for IsActive
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

**3. Configure Mapping (if names differ) using MappingUtils:**

```csharp
var userToEntityMap = MappingUtils.BuildMemberMap<User, UserEntity>(user =>
    new UserEntity
    {
        UserId = user.Id,
        UserName = user.Name,
        Enabled = user.IsActive,
        DateOfBirth = user.BirthDate
        // Properties with the same name are automatically handled by BuildMemberMap if assigned
        // (though ExpressionConverter itself would match them by name if not in a map).
    });
```

**4. Convert the Expression:**

```csharp
// Convert using the memberMap generated by MappingUtils
Expression<Func<UserEntity, bool>> entityPredicate =
    ExpressionConverter.Convert<User, UserEntity>(isActiveAdultUserDomainFilter, userToEntityMap);
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

---

## 🛠️ Advanced Usage: `memberMap` and `customMap`

The `ExpressionConverter.Convert` method takes two optional parameters for controlling the mapping:

* **`IDictionary<string, string>? memberMap = null`**:
  * This dictionary allows you to explicitly define mappings between source member names (keys) and destination member names (values) for direct members of `TSource`.
  * The `MappingUtils.BuildMemberMap` helper is the recommended way to create this dictionary.
  * If a source member name is found in this dictionary, its value will be used as the target member name on the destination type.
  * This takes precedence over automatic name matching for members of `TSource`.
  * If a member of `TSource` is not in the map, automatic name matching is attempted.
  * Example (manual creation): `new Dictionary<string, string> { { "SourcePropertyName", "DestinationPropertyName" } }`

* **`Func<MemberExpression, ParameterExpression, Expression?>? customMap = null`**:
  * This delegate provides the ultimate control for complex mapping scenarios where direct name or dictionary mapping is insufficient.
  * The function is called for each `MemberExpression` in the source predicate (e.g., `user.IsActive`, or even `user.Address.Street`).
  * **Parameters**:
    * `sourceMemberExpr`: The original `MemberExpression` from the source predicate (e.g., `user.IsActive` or `user.Address.Street`).
    * `destParamExpr`: The `ParameterExpression` for the destination type (e.g., `entity` of type `UserEntity`).
  * **Return Value**:
    * If you return a non-null `Expression`, that expression will be used directly as the replacement in the new expression tree for that specific `sourceMemberExpr`. This allows you to, for example, call a method on the destination, combine multiple destination properties, perform transformations, or handle complex path remapping (e.g., `s.Child.Name` to `d.ChildName`).
    * If you return `null`, the converter will fall back to its default logic for that specific member (which involves using `memberMap` for direct members of `TSource`, or direct name matching for nested members or members of captured variables).
  * This `customMap` has the highest precedence in the mapping logic. The `customMap` has the highest precedence in the mapping logic and can handle complex scenarios like nested properties or captured variables.

### Custom Mapping Example:

Map `SourceType.Data` to `DestinationType.ProcessedData`.

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
```

A more complex `customMap` might transform the value itself or map to a method, e.g.:

```csharp
Expression<Func<SourceType, bool>> complexFilter = s => s.NumericValue > 10;
Func<MemberExpression, ParameterExpression, Expression?> complexCustomMap = (srcMember, destParam) =>
{
    if (srcMember.Member.Name == "NumericValue")
    {
        // Map s.NumericValue to d.CalculationResult (which might be int)
        // This custom map returns d.CalculationResult. The "> 10" is applied afterwards.
        PropertyInfo destProp = typeof(DestinationTypeWithCalc).GetProperty("CalculationResult");
        return Expression.Property(destParam, destProp);
    }
    return null;
};
```

This would convert `complexFilter` to `d => d.CalculationResult > 10`.

---

## 🛣️ Roadmap & Future Enhancements (Post v0.2.1)

While `CrossTypeExpressionConverter v0.2.1` focuses on robust predicate conversion with flexible mapping, future versions may include:

* ExpressionConverterOptions Object: Introduce a dedicated options class to simplify the Convert method signature and allow for more configuration points (e.g., ThrowOnFailedMemberMapping toggle, case-sensitivity options for name matching).
* Selector Conversion: Support for converting projection expressions (e.g., Expression<Func<TSource, TResult>> to Expression<Func<TDestination, TDestResult>>).
* Order By Conversion: Support for key selector expressions for ordering.
* Fluent Configuration API: A fluent interface for defining mappings.
* Performance Caching: Internal caching of MemberInfo and type mapping details.
* Attribute-Based Mapping: Define mappings via attributes on type members.
* Roslyn Analyzer: For compile-time diagnostics of potential mapping issues.

---

## 🤝 Contributing

Contributions are welcome! If you have an idea for a new feature, an improvement, or a bug fix, please:

* Check the [Issues](https://www.google.com/search?q=https://github.com/scherenhaenden/CrossTypeExpressionConverter/issues) to see if your idea or bug has already been discussed.
* If not, open a new issue to discuss the change.
* Fork the repository, make your changes in a feature branch, and submit a pull request with a clear description of your changes.

Please ensure that any new code includes appropriate unit tests.

---

## ❓ FAQ

* **Q: Does this work with complex nested objects?**
  * A: Yes, for member access chains like `source.Order.Details.ProductName`. The converter will attempt to map each segment based on its default logic (name matching for nested parts) or what your `customMap` provides. For instance, if `customMap` isn't used for `source.Order` and it's mapped to `dest.CustomerOrder` (via `memberMap` or name), the converter will then try to resolve `Details` on `CustomerOrder`'s type, and so on. Complex re-structuring of nested paths (e.g., flattening `source.Order.Details.ProductName` to `dest.ProductName`) would typically require `customMap` to handle the full `source.Order.Details.ProductName` expression.

* **Q: What happens if a property doesn't exist on the destination type and isn't mapped?**
  * A: The converter will throw an `InvalidOperationException` detailing which member could not be mapped. Currently, this behavior is not configurable.

* **Q: Is this similar to AutoMapper's `ProjectTo`?**
  * A: It shares the goal of translating expressions for ORM querying. However, `CrossTypeExpressionConverter` is a more focused utility for converting individual `Expression<Func<TSource, bool>>`. It doesn't perform full object-to-object mapping or offer `IQueryable` extension methods like `ProjectTo` out-of-the-box, but it can be a powerful component in building such systems or for more direct expression manipulation.

---

## ⚖️ License

This project is licensed under the **MIT License**. See the [LICENSE](https://www.google.com/url?sa=E&source=gmail&q=https://github.com/scherenhaenden/CrossTypeExpressionConverter/blob/main/LICENSE) file for details.

---

Copyright (c) 2025 scherenhaenden