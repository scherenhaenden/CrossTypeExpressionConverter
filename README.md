# CrossTypeExpressionConverter

[![NuGet Version](https://img.shields.io/nuget/v/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CrossTypeExpressionConverter.svg?style=flat-square)](https://www.nuget.org/packages/CrossTypeExpressionConverter/)
[![Release](https://img.shields.io/github/actions/workflow/status/scherenhaenden/CrossTypeExpressionConverter/github-release.yml?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/actions/workflows/github-release.yml)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](https://github.com/scherenhaenden/CrossTypeExpressionConverter/blob/master/LICENSE)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=scherenhaenden_CrossTypeExpressionConverter&metric=alert_status)](https://sonarcloud.io/dashboard?id=scherenhaenden_CrossTypeExpressionConverter)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=scherenhaenden_CrossTypeExpressionConverter&metric=coverage)](https://sonarcloud.io/dashboard?id=scherenhaenden_CrossTypeExpressionConverter)

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)
![C#](https://img.shields.io/badge/C%23-latest-239120?style=flat-square&logo=c-sharp&logoColor=white)
![OS](https://img.shields.io/badge/OS-Windows%20%7C%20macOS%20%7C%20Linux-000000?style=flat-square)

**CrossTypeExpressionConverter** is a lightweight, high-performance .NET library designed to seamlessly translate LINQ predicate expressions (`Expression<Func<TSource, bool>>`) from a source type (`TSource`) to an equivalent expression for a destination type (`TDestination`). 

This is particularly useful in layered architectures (e.g., Clean Architecture, DDD) where query filters defined on domain models need to be applied directly to database entities (Data DTOs/Entities) in Entity Framework Core or other ORMs, avoiding manual rewrite of query logic.

---

## 🌟 Key Features

* **🔒 Type-Safe Conversion**: Translates LINQ expressions at compile-time, reducing runtime query mapping bugs.
* **⚡ IQueryable Compatibility**: Translated expressions are fully compiled into SQL by ORMs (like EF Core), performing filtration server-side.
* **🧬 Multi-Tier Mapping Resolution**:
  * **Automatic Name Matching**: Properties with matching names map automatically.
  * **Strongly-Typed Projection Maps**: Generate maps using `MappingUtils.BuildMemberMap` via object initializers.
  * **Explicit Dictionaries**: Pass custom dictionaries to translate differing property names.
  * **Attributes (`[MapsTo]`)**: Annotate source models to declare destination property names directly.
  * **Custom Delegates (`CustomMap`)**: Supply lambdas to perform complex tree transformations, path flattening, or type modifications.
* **🌳 Nested Object Graph Support**: Recursively traverses and re-maps nested property paths.
* **📦 Closed-Over Variable Support**: Safely preserves references to captured local variables, letting your ORM parameterize database queries correctly.
* **🚀 MemoryCache Optimization**: Uses internal, bounded sliding caches to eliminate reflection overhead on repeated translations.

---

## 💾 Installation

Install via the .NET CLI:
```shell
dotnet add package CrossTypeExpressionConverter --version 0.6.0
```
Or via the NuGet Package Manager Console:
```shell
Install-Package CrossTypeExpressionConverter -Version 0.6.0
```

---

## 🚀 Quick Start in 60 Seconds

### 1. Define Your Models & Map
```csharp
// Domain Model (Clean)
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsAvailable { get; set; }
}

// Entity Model (DB Representation)
public class ProductEntity {
    public int ProductId { get; set; }
    public string Name { get; set; }        // Mapped automatically (exact match)
    public bool Active { get; set; }
}

// Generate the member mapping dictionary
var productMap = MappingUtils.BuildMemberMap<Product, ProductEntity>(p => new ProductEntity {
    ProductId = p.Id,
    Active = p.IsAvailable
});
```

### 2. Convert and Query
```csharp
using CrossTypeExpressionConverter;

// 1. Define a domain filter
Expression<Func<Product, bool>> domainFilter = p => p.IsAvailable && p.Id > 10;

// 2. Convert it using the facade (or via Dependency Injection)
Expression<Func<ProductEntity, bool>> entityFilter =
    ExpressionConverterFacade.Convert<Product, ProductEntity>(domainFilter, productMap);

// 3. Query the DB (EF Core compiles this entirely into parameterized SQL)
var products = dbContext.Products.Where(entityFilter).ToList();
```

---

## 📚 Detailed Documentation Directory

For deep dives into configuration, architecture, and edge cases, see our sub-documentation:

* **[Getting Started Guide](docs/getting-started.md)**: Extended installation, target framework specifications, and complete step-by-step setup guides.
* **[Mapping Strategies & Precedence](docs/mapping-strategies.md)**: How to configure mappings using dictionary initializers, custom attributes, custom conversion delegates, and mapping resolution rules.
* **[Advanced Features & ORMs](docs/advanced-features.md)**: Handling nested properties, captured variables (closures), and testing or troubleshooting EF Core compilation.
* **[Architecture, Dependency Injection & Caching](docs/architecture-and-di.md)**: Decoupling code via `IExpressionConverter`, registering as a Singleton in ASP.NET Core DI, and configuring memory caches or cache priming.
* **[Error Handling & Troubleshooting](docs/error-handling.md)**: Customizing unmapped property responses (`Throw` vs `ReturnDefault`) and debug guidelines for common errors.
* **[API Reference](docs/api-reference.md)**: Formal declaration of classes, methods, parameters, and return types.

---

## 🧪 Running Tests Locally

The test suite uses [NUnit](https://nunit.org/) with [coverlet](https://github.com/coverlet-coverage/coverlet) for code coverage:

```shell
# Restore dependencies
dotnet restore

# Run all tests
dotnet test

# Run tests with code coverage report
dotnet test --collect:"XPlat Code Coverage" --verbosity normal
```

Coverage reports are generated in `CrossTypeExpressionConverter.Tests/**/coverage.cobertura.xml`.

---

## 🤝 Contributing

Contributions are welcome! If you have ideas for features or find bugs:
1. Search our [GitHub Issues](https://github.com/scherenhaenden/CrossTypeExpressionConverter/issues) to verify if the topic is already active.
2. Fork the repository, create a descriptive feature branch, and submit a Pull Request.
3. Ensure any new logic is backed by appropriate unit tests.

---

## ⚖️ License

Licensed under the [MIT License](LICENSE). 

Copyright (c) 2025 Edward S Flores