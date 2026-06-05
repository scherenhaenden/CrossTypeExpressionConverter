# Getting Started with CrossTypeExpressionConverter

This guide will walk you through installing the library, setting up your project, and executing your first LINQ expression conversion.

---

## 💾 Installation

`CrossTypeExpressionConverter` is distributed as a NuGet package. You can install it using one of the following methods:

### .NET CLI
Run the following command in your terminal at the root of your project:
```shell
dotnet add package CrossTypeExpressionConverter --version 0.6.0
```

### NuGet Package Manager Console
Run this command in the Package Manager Console in Visual Studio:
```shell
Install-Package CrossTypeExpressionConverter -Version 0.6.0
```

### PackageReference (MSBuild)
Add the following line directly to your `.csproj` file inside an `<ItemGroup>`:
```xml
<PackageReference Include="CrossTypeExpressionConverter" Version="0.6.0" />
```

---

## ⚙️ Requirements & Compatibility

- **Target Framework**: The library targets `.NET 10.0`. Your project must reference `.NET 10.0` or later to consume this package.
- **Dependencies**: Relies on `Microsoft.Extensions.Caching.Memory` (version `8.0.1` or later) for caching attribute mappings.
- **ORM Compatibility**: Fully compatible with Entity Framework Core (EF Core) and other LINQ providers that compile expression trees down to database queries.

---

## 🚀 Quick Start Tutorial

This tutorial demonstrates how to define a reusable query filter in your domain layer and automatically convert it into a database-compatible format.

### Step 1: Define Your Models

Often, your domain layer uses clean models, while your data access layer (EF Core) uses tables with different naming conventions.

```csharp
// Domain Model (Clean, business-focused)
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime BirthDate { get; set; }
}

// Database Entity (Database schema representation)
public class UserEntity
{
    public int UserId { get; set; }          // Mapped from Id
    public string? UserName { get; set; }     // Mapped from Name
    public bool Enabled { get; set; }         // Mapped from IsActive
    public DateTime DateOfBirth { get; set; } // Mapped from BirthDate
}
```

### Step 2: Define a Domain Filter Expression

Define a type-safe filter on your domain model. This filter can live in your core application service or domain logic:

```csharp
using System;
using System.Linq.Expressions;

// A reusable filter representing "Active adult users"
Expression<Func<User, bool>> domainFilter = user => 
    user.IsActive && user.BirthDate <= DateTime.Today.AddYears(-18);
```

### Step 3: Establish the Property Map

Since the property names differ between `User` and `UserEntity`, you must specify how to map them. The easiest way is using `MappingUtils.BuildMemberMap`, which extracts mappings from a strongly-typed projection expression:

```csharp
using CrossTypeExpressionConverter;

var userToEntityMap = MappingUtils.BuildMemberMap<User, UserEntity>(user =>
    new UserEntity
    {
        UserId = user.Id,
        UserName = user.Name,
        Enabled = user.IsActive,
        DateOfBirth = user.BirthDate
    });
```
*Note: Any properties with identical names will be automatically matched if they are not specified in the mapping dictionary.*

### Step 4: Convert the Filter Expression

Now, use the static `ExpressionConverterFacade` to convert the domain-level filter into an entity-level filter:

```csharp
Expression<Func<UserEntity, bool>> databaseFilter =
    ExpressionConverterFacade.Convert<User, UserEntity>(domainFilter, userToEntityMap);
```

Under the hood, the converter parses the expression tree of `domainFilter`, matches the properties based on your map, and rebuilds the tree targeting `UserEntity`.

> **Tip:** You can also use the instance-based `ExpressionConverter` class (recommended for production applications with Dependency Injection). See [Architecture & Dependency Injection](architecture-and-di.md) for details.

### Step 5: Query Your Database via EF Core

Pass the translated expression directly to your ORM. Since the returned object is a native `Expression<Func<UserEntity, bool>>`, EF Core will translate it entirely to SQL and run it server-side:

```csharp
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class UserRepository
{
    private readonly DbContext _dbContext;

    public UserRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<UserEntity> GetAdultUsers(Expression<Func<UserEntity, bool>> filter)
    {
        // EF Core translates this directly into a SQL query:
        // SELECT * FROM Users WHERE Enabled = 1 AND DateOfBirth <= ...
        return _dbContext.Set<UserEntity>()
                         .Where(filter)
                         .ToList();
    }
}
```

---

## 🗺️ Next Steps

To learn more about configuring mappings, handling edge cases, and registering the converter in your Dependency Injection container, see the following guides:

- [Mapping Strategies](mapping-strategies.md): Understand the four mapping layers (automatic name matching, dictionaries, attributes, and custom delegates).
- [Advanced Features](advanced-features.md): Support for nested property paths, captured local variables, and Entity Framework Core optimization.
- [Architecture & Dependency Injection](architecture-and-di.md): Register the converter as a service, use instance methods, configure caching, and manage performance.
- [Error Handling](error-handling.md): Configure strategies for missing properties and debug translation errors.

---

<p align="center">
  <a href="README.md">📚 Documentation Index</a>
</p>
