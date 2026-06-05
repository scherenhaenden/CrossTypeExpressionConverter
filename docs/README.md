# CrossTypeExpressionConverter — Documentation Index

Welcome to the documentation for **CrossTypeExpressionConverter**. Below you will find detailed guides covering every aspect of the library, from initial setup to advanced expression tree translation techniques.

> **Looking for the main project page?** See the [root README](../README.md) for a quick overview, badges, and installation instructions.

---

## 📚 Documentation Sections

| Guide | Description |
|-------|-------------|
| 🚀 **[Getting Started](getting-started.md)** | Install the NuGet package, verify target framework requirements, and walk through a complete step-by-step Quick Start tutorial. |
| 🗺️ **[Mapping Strategies & Precedence](mapping-strategies.md)** | Understand the four mapping layers — automatic name matching, dictionaries, `[MapsTo]` attributes, and custom delegates — with a full precedence flow diagram. |
| 🛣️ **[Advanced Features & ORM Integration](advanced-features.md)** | Deep dive into nested property path traversal, captured variable (closure) handling, and end-to-end Entity Framework Core integration with SQL parameterization. |
| 🏛️ **[Architecture, DI & Caching](architecture-and-di.md)** | Explore the class diagram, register `IExpressionConverter` in ASP.NET Core DI, configure the fluent options API, and understand the internal `MemoryCache`-based reflection cache. |
| ⚠️ **[Error Handling & Troubleshooting](error-handling.md)** | Configure `Throw` vs `ReturnDefault` error strategies, understand silent filter modification risks, and resolve common exceptions with a step-by-step checklist. |
| 📖 **[API Reference](api-reference.md)** | Formal declarations of all public interfaces, classes, methods, attributes, and enums with full constructor and method signatures. |

---

## 🗂️ Project Structure

```
CrossTypeExpressionConverter/
├── CrossTypeExpressionConverter/          # Library source code
│   ├── ExpressionConverter.cs             # Core converter (implements IExpressionConverter)
│   ├── ExpressionConverterFacade.cs       # Static facade for backward compatibility
│   ├── ExpressionConverterOptions.cs      # Fluent configuration options
│   ├── IExpressionConverter.cs            # Public interface for DI
│   ├── MappingUtils.cs                    # Helper to build member map dictionaries
│   ├── MapsToAttribute.cs                 # [MapsTo] attribute for declarative mapping
│   ├── MemberMappingCache.cs              # Internal MemoryCache for attribute lookups
│   └── MemberMappingErrorHandling.cs      # Enum: Throw | ReturnDefault
├── CrossTypeExpressionConverter.Tests/    # NUnit test suite
│   ├── Helpers/Models/                    # Test model classes
│   └── Units/                             # Unit test classes
├── docs/                                  # ← You are here
├── .github/workflows/                     # CI/CD (tests, release, publish)
├── README.md                              # Project landing page
├── LICENSE                                # MIT License
└── CrossTypeExpressionConverter.sln       # Solution file
```