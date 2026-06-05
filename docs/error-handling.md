# Error Handling & Troubleshooting

This guide explains how `CrossTypeExpressionConverter` behaves when a property cannot be mapped, how to configure error strategies, and how to resolve common conversion issues.

---

## ⚠️ Error Handling Strategies (`MemberMappingErrorHandling`)

You can control how the converter reacts to unmapped properties by setting `ErrorHandling` in `ExpressionConverterOptions`.

### 1. `MemberMappingErrorHandling.Throw` (Default)
If the converter encounters a property in the source expression that cannot be resolved on the destination type (and is not mapped via a custom delegate, dictionary, or attribute), it throws an `InvalidOperationException`.

- **Rationale**: Safe by default. Failing fast ensures that queries do not execute with missing filters, which could lead to unauthorized data access or incorrect results.
- **Exception Message Format**:
  ```text
  Member 'BirthDate' from source type 'MyApp.Domain.User' (attempting to map to 'BirthDate') could not be mapped because the destination member was not found on type 'MyApp.Data.UserEntity'. Full source member expression being processed: user.BirthDate
  ```

---

### 2. `MemberMappingErrorHandling.ReturnDefault`
If the converter encounters an unmapped property, it ignores the missing field and replaces that sub-expression with the default value of the property's type (e.g., `null` for strings, `0` for integers, `false` for booleans).

```csharp
var options = new ExpressionConverterOptions()
    .WithErrorHandling(MemberMappingErrorHandling.ReturnDefault);
```

> [!WARNING]
> Use `ReturnDefault` with extreme caution. It alters the logic of your filters at runtime.

#### Scenario: Silent Filter Modification
Consider the source predicate:
```csharp
Expression<Func<User, bool>> filter = user => user.IsActive && user.SecretCode == "ADMIN123";
```
If `SecretCode` does not exist on `UserEntity` and error handling is set to `ReturnDefault`, the generated expression becomes:
```csharp
entity => entity.Enabled && default(string) == "ADMIN123"
```
Since `default(string)` is `null`, `null == "ADMIN123"` evaluates to `false`. The query effectively becomes:
```csharp
entity => entity.Enabled && false
```
This query will return **no records**, even if active users with that secret code exist, without raising any errors.

---

## 🔍 Troubleshooting Common Issues

### Issue 1: "Member 'X' ... could not be mapped because the destination member was not found on type 'Y'"

This error occurs when the converter cannot locate a public instance property or field matching the target name.

#### Checklist to Resolve:
1. **Case Sensitivity**: Check the casing. Property lookup is case-sensitive. E.g., `user.id` will not match `UserId` or `Id` automatically.
2. **Access Modifiers**: Ensure the property on the destination type has a `public` getter and setter and is an instance member (not `static`).
3. **Map Verification**: If using `MemberMap`, ensure the key is the exact name of the source property, and the value is the exact name of the destination property.
4. **Attribute Check**: If using `[MapsTo]`, verify that the name passed to the attribute constructor exactly matches the destination property name (use `nameof` to prevent typos, e.g., `[MapsTo(nameof(Destination.TargetProp))]`).

---

### Issue 2: "Mapping must use an object-initializer"

This exception is thrown by `MappingUtils.BuildMemberMap` at startup.

#### Cause:
The lambda expression passed to `BuildMemberMap` does not conform to the expected format. It must look like: `src => new Dest { DestProp = src.SrcProp }`.

#### Examples of Invalid Expressions:
```csharp
// ❌ Invalid: Uses constructor parameters
var map = MappingUtils.BuildMemberMap<User, UserEntity>(u => new UserEntity(u.Id, u.Name));

// ❌ Invalid: Contains block statements
var map = MappingUtils.BuildMemberMap<User, UserEntity>(u => {
    var entity = new UserEntity { UserId = u.Id };
    return entity;
});

// ❌ Invalid: Right-hand side is a method call or nested property
var map = MappingUtils.BuildMemberMap<User, UserEntity>(u => new UserEntity {
    UserName = u.Name.ToUpper(),
    Zip = u.Address.ZipCode
});
```

#### Valid Expression:
```csharp
//  Valid: Clean object initializer with direct property assignments
var map = MappingUtils.BuildMemberMap<User, UserEntity>(u => new UserEntity {
    UserId = u.Id,
    UserName = u.Name
});
```
*Note: For complex conversions like `u.Name.ToUpper()` or path remapping like `u.Address.ZipCode`, use a `CustomMap` delegate in your options rather than `MappingUtils`.*

---

### Issue 3: `ArgumentNullException: Value cannot be null. (Parameter 'options')`

This exception is thrown by the `ExpressionConverter` constructor.

#### Cause:
You passed `null` as the `ExpressionConverterOptions` argument when creating an `ExpressionConverter` instance.

#### Fix:
Always provide a valid options object. If you need default settings, use `ExpressionConverterOptions.Default`:
```csharp
// ✅ Correct
var converter = new ExpressionConverter(ExpressionConverterOptions.Default);

// ❌ Will throw
var converter = new ExpressionConverter(null!); // ArgumentNullException
```

---

<p align="center">
  <a href="README.md">📚 Documentation Index</a>
</p>
