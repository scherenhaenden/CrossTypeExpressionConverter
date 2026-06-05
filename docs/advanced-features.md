# Advanced Features & ORM Integration

This guide explores how `CrossTypeExpressionConverter` handles complex LINQ expression structures, resolves closed-over variables, and integrates with ORMs like Entity Framework Core (EF Core) for server-side evaluation.

---

## 1. Nested Properties

In real-world applications, expressions often access nested objects (e.g., `customer => customer.Address.City == "Madrid"`). 

### How it Works Internally
When translating member accesses, the converter processes the expression recursively. For a nested chain like `s.Address.City`:
1. It reaches the parameter `s` (type `TSource`) and converts it to `d` (type `TDestination`).
2. It translates the property `Address` on `TSource` to `Address` (or its mapped name) on `TDestination`.
3. It determines the type of `d.Address`.
4. It translates `City` on the source `Address` type to `City` (or its mapped name) on the destination `Address` type.

### Example
Suppose we have nested objects with different names:

```csharp
public class UserSource
{
    public ContactInfoSource Contact { get; set; }
}
public class ContactInfoSource
{
    public string EmailAddress { get; set; }
}

public class UserEntity
{
    public ContactInfoEntity ContactInfo { get; set; } // Different name (Contact -> ContactInfo)
}
public class ContactInfoEntity
{
    public string Email { get; set; } // Different name (EmailAddress -> Email)
}
```

We configure the member maps for both layers:
```csharp
// Top level map
var userMap = new Dictionary<string, string> { { "Contact", "ContactInfo" } };

// Nested level map (if custom mappings are needed inside nested properties)
// Note: If you don't map ContactInfoEntity properties directly in CustomMap, 
// they default to name-matching. Let's write a CustomMap for path rewriting:
Func<MemberExpression, ParameterExpression, Expression?> customMap = (srcMember, destParam) =>
{
    if (srcMember.Member.Name == nameof(ContactInfoSource.EmailAddress) &&
        srcMember.Expression is MemberExpression innerExpr &&
        innerExpr.Member.Name == nameof(UserSource.Contact))
    {
        // Reconstruct: destParam.ContactInfo.Email
        var contactInfoProp = typeof(UserEntity).GetProperty(nameof(UserEntity.ContactInfo));
        var emailProp = typeof(ContactInfoEntity).GetProperty(nameof(ContactInfoEntity.Email));
        
        var contactAccess = Expression.Property(destParam, contactInfoProp);
        return Expression.Property(contactAccess, emailProp);
    }
    return null;
};

Expression<Func<UserSource, bool>> filter = u => u.Contact.EmailAddress == "test@test.com";

var options = new ExpressionConverterOptions().WithCustomMap(customMap);
var converted = ExpressionConverterFacade.Convert<UserSource, UserEntity>(filter, options);
// Output predicate: u => u.ContactInfo.Email == "test@test.com"
```

---

## 2. Captured Variables (Closed-Over Variables)

A common pattern is comparing a property against a local variable or a parameter from another scope:

```csharp
int targetAge = 25;
Expression<Func<User, bool>> filter = u => u.Age > targetAge;
```

### The Captured Variable Problem
In C#, variables like `targetAge` are compiled into fields of an internal, compiler-generated class (a closure). In the expression tree, `targetAge` is represented as a `MemberExpression` targeting a compiler-generated class instance, *not* the parameter `u`.

### How the Library Handles Closures
The converter's `ExpressionVisitor` processes member access chains recursively via `VisitMember`. When it encounters a `MemberExpression` whose root expression is **not** the source parameter `TSource`, the visitor does not attempt to remap it. Specifically:

1. The `VisitParameter` method only replaces parameters of type `TSource`. Parameters belonging to compiler-generated closure classes pass through unchanged.
2. When `VisitMember` recurses into `node.Expression`, it eventually reaches the closure's constant expression (a `ConstantExpression` holding the closure instance), which is returned as-is.
3. The member access (e.g., `closure.targetAge`) is preserved in the output tree, allowing the LINQ provider (such as EF Core) to evaluate it as a SQL query parameter at execution time.

```csharp
int targetAge = 25;
Expression<Func<User, bool>> domainFilter = u => u.Age > targetAge;

// The converter converts "Age" to "UserAge" but leaves "targetAge" untouched.
var converted = ExpressionConverterFacade.Convert<User, UserEntity>(domainFilter, new Dictionary<string, string>
{
    { "Age", "UserAge" }
});
// Resulting Expression: u => u.UserAge > targetAge
```

---

## 3. Entity Framework Core (EF Core) Integration

When using expressions in database queries, it is critical that the expressions compile to database SQL and are executed on the server, rather than loading all records into memory (client-side evaluation).

### SQL Parameterization
Because `CrossTypeExpressionConverter` yields a clean `Expression<Func<TDestination, bool>>` tree, ORMs like EF Core can inspect the tree, recognize the parameter substitutions and closures, and compile them into parameterized SQL:

```csharp
// Domain Level Filter
int categoryId = 5;
Expression<Func<Product, bool>> domainFilter = p => p.IsAvailable && p.CategoryId == categoryId;

// Translate mappings (e.g., IsAvailable -> Active)
var map = new Dictionary<string, string> { { "IsAvailable", "Active" } };
var dbFilter = ExpressionConverterFacade.Convert<Product, ProductEntity>(domainFilter, map);

// Execution
var results = dbContext.Products.Where(dbFilter).ToList();
```

**EF Core Generates SQL similar to:**
```sql
SELECT [p].[Id], [p].[Active], [p].[CategoryId], [p].[Name]
FROM [Products] AS [p]
WHERE ([p].[Active] = CAST(1 AS bit)) AND ([p].[CategoryId] = @__categoryId_0)
```
Notice that:
1. `IsAvailable` was successfully mapped to `Active` on the database table.
2. `categoryId` was parameterized as `@__categoryId_0` to prevent SQL injection and leverage query plan caching.
3. The filter was completely executed on the database server.

---

<p align="center">
  <a href="README.md">📚 Documentation Index</a>
</p>
