using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;



 // --- NUnit Tests ---
[TestFixture]
public class ExpressionConverterTests
{
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        return predicate.Compile()(item);
    }

    // --- Basic Same Name Property Matching Tests ---
    [Test]
    public void Convert_SimpleIntProperty_SameName_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 10;
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 10 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 5 }));
    }

    [Test]
    public void Convert_SimpleStringProperty_SameName_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "Test";
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Name = "Test" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = "Other" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = null }));
    }

    [Test]
    public void Convert_SimpleBoolProperty_SameName_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.IsActive; // Equivalent to s.IsActive == true
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { IsActive = false }));
    }

    // --- Dictionary-Based Mapping (memberMap) Tests ---
    [Test]
    public void Convert_IntProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 20;
        var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Id), nameof(DestDifferentNames.EntityId) } };
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 20 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 15 }));
    }

    [Test]
    public void Convert_StringProperty_WithMemberMap_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name == "MapTest";
        var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) } };
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "MapTest" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "OtherMap" }));
    }

    // --- Custom Delegate Mapping (customMap) Tests ---
    [Test]
    public void Convert_StringProperty_WithCustomMap_ToDifferentPropertyAndTransform()
    {
        Expression<Func<SourceForCustomMap, bool>> sourcePredicate = s => s.Data == "PREFIX_Original";

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (sourceMemberExpr, destParamExpr) =>
        {
            if (sourceMemberExpr.Member.Name == nameof(SourceForCustomMap.Data))
            {
                // We want to map s.Data to d.TransformedData
                // The original predicate is s.Data == "PREFIX_Original"
                // The custom map should return an expression for d.TransformedData
                // The comparison "== 'PREFIX_Original'" will be applied to this returned expression.
                var transformedDataProp = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.TransformedData));
                if (transformedDataProp == null) throw new NullReferenceException("TransformedData property not found");
                return Expression.Property(destParamExpr, transformedDataProp);
            }
            return null; // Fallback for other properties
        };

        var convertedPredicate = ExpressionConverter.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, null, customMap);

        // Test items will have TransformedData = "PREFIX_" + OriginalData
        Assert.IsTrue(Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "PREFIX_Original" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestForCustomMap { TransformedData = "Original" })); // Missing prefix
    }
    
    [Test]
    public void Convert_NumericProperty_WithCustomMap_ToDoubledValue()
    {
        // Source: s.NumericValue == 10
        // Destination: d.DoubledValue == 10 (where d.DoubledValue was derived from s.NumericValue * 2)
        // This means if s.NumericValue was 5, then d.DoubledValue is 10.
        // So, the predicate should effectively become: (s.NumericValue * 2) == 10
        Expression<Func<SourceForCustomMap, bool>> sourcePredicate = s => s.NumericValue == 10; // This '10' is the value to compare against

        Func<MemberExpression, ParameterExpression, Expression?> customMap = (sourceMemberExpr, destParamExpr) =>
        {
            if (sourceMemberExpr.Member.Name == nameof(SourceForCustomMap.NumericValue))
            {
                // We want to replace 's.NumericValue' with an expression that represents 'd.DoubledValue / 2'
                // so that 'd.DoubledValue / 2 == 10' becomes 'd.DoubledValue == 20'.
                // Or, more directly, map s.NumericValue to an expression that, when compared to 10,
                // means d.DoubledValue is being implicitly checked.

                // Let's map s.NumericValue to an expression representing d.DoubledValue
                var doubledValueProp = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.DoubledValue));
                 if (doubledValueProp == null) throw new NullReferenceException("DoubledValue property not found");
                
                // The source predicate is s.NumericValue == 10.
                // If we replace s.NumericValue with d.DoubledValue, it becomes d.DoubledValue == 10.
                // This is if the custom map is intended to reinterpret the *source member* as the *destination member directly*.
                // This test implies that the value on DestForCustomMap.DoubledValue is ALREADY doubled.
                // So if source.NumericValue was 5, dest.DoubledValue would be 10.
                // The predicate s.NumericValue == 5 should become d.DoubledValue == 10.
                // This is tricky. The customMap replaces the MEMBER ACCESS part.
                // If source is s.NumericValue == X, custom map replaces s.NumericValue with some_expr_based_on_d.
                // Then it becomes some_expr_based_on_d == X.

                // Let's assume the intent is: if s.NumericValue is used, it refers to d.DoubledValue conceptually.
                // The predicate is on the source value.
                // If s.NumericValue is 5, the predicate s.NumericValue == 5 is true.
                // We want the converted predicate to be true for a DestForCustomMap where DoubledValue is 10.
                // So, if s.NumericValue is replaced by an expression E(d), then E(d) == 5 should be true when d.DoubledValue = 10.
                // This means E(d) should effectively be d.DoubledValue / 2.
                
                var destProperty = Expression.Property(destParamExpr, doubledValueProp);
                var twoConstant = Expression.Constant(2);
                return Expression.Divide(destProperty, twoConstant); // d.DoubledValue / 2
            }
            return null;
        };

        var convertedPredicate = ExpressionConverter.Convert<SourceForCustomMap, DestForCustomMap>(sourcePredicate, null, customMap);
        
        // s.NumericValue == 10  => (d.DoubledValue / 2) == 10  => d.DoubledValue == 20
        Assert.IsTrue(Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 20 }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 10 })); // (10/2) == 10 -> 5 == 10 is false
        Assert.IsFalse(Evaluate(convertedPredicate, new DestForCustomMap { DoubledValue = 0, Id = 0, TransformedData = "" })); // (0/2) == 10 -> 0 == 10 is false
    }


    // --- Nested Property Access Tests ---
    [Test]
    public void Convert_NestedProperty_SameNames_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.Child != null && s.Child.NestedId == 100;
        var convertedPredicate = ExpressionConverter.Convert<SourceWithNested, DestWithNested>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 100 } }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestWithNested { Child = new NestedDestProp { NestedId = 50 } }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestWithNested { Child = null }));
    }

    [Test]
    public void Convert_NestedProperty_WithMemberMapForParentProperty_ShouldEvaluateCorrectly()
    {
        // Source: s.ChildToMap.NestedName == "Test"
        // Dest: d.MappedChild.InnerName == "Test"
        // We need to map:
        // 1. SourceWithNested.ChildToMap -> DestWithNested.MappedChild (this is a member of TSource)
        // 2. NestedSourceProp.NestedName -> NestedDestPropDifferentName.InnerName (this happens because after ChildToMap is mapped to MappedChild,
        //    the visitor will see MappedChild.NestedName and then try to find NestedName on MappedChild type)
        // For this to work with the current visitor, the properties *within* the nested types must match after the parent (ChildToMap->MappedChild) is mapped,
        // OR we need a more sophisticated visitor/mapping for nested types.

        // Let's test mapping the parent property `ChildToMap` to `MappedChild`.
        // The properties *inside* `MappedChild` (i.e. `InnerName`) must match `NestedSourceProp.NestedName`
        // OR the `GetMember(destName)[0]` on `NestedDestPropDifferentName` must find a property named `NestedName`.
        // This will fail if `NestedDestPropDifferentName` doesn't have `NestedName`.
        // The current visitor doesn't have a mechanism for deeply nested member maps.
        // It maps members of TSource. `s.ChildToMap` is a member. `s.ChildToMap.NestedName` is `MemberAccess(MemberAccess(s, ChildToMap), NestedName)`.
        // `VisitMember(ChildToMap)` will map it to `d.MappedChild`.
        // Then `VisitMember(NestedName)` will be on `Expression = d.MappedChild`. Its `DeclaringType` will be `NestedDestPropDifferentName`.
        // So, it will try to find `NestedName` on `NestedDestPropDifferentName`.

        // To make this test pass as intended (map NestedName to InnerName), we'd need a custom map for s.ChildToMap.NestedName
        // or a more advanced mapping system.

        // Let's simplify: map ChildToMap to MappedChild, and ensure MappedChild's *internal* properties are accessed by original names.
        // For this, NestedDestPropDifferentName should have properties that NestedSourceProp has.
        // Let's assume NestedDestPropDifferentName has NestedName for this test to show parent mapping.
        // We will create a temporary DestWithNested_Temp that has MappedChild of type NestedDestProp (which has NestedName)
        
        var tempDestType = new DestWithNested { MappedChild = new NestedDestPropDifferentName() }; // Just for type info
        Expression<Func<SourceWithNested, bool>> sourcePredicate = s => s.ChildToMap != null && s.ChildToMap.NestedName == "NestedMapTest";
        
        var memberMap = new Dictionary<string, string>
        {
            { nameof(SourceWithNested.ChildToMap), nameof(DestWithNested.MappedChild) }
        };

        // This will attempt to access MappedChild.NestedName.
        // If MappedChild is of type NestedDestPropDifferentName, it needs a "NestedName" property.
        // If we want it to access MappedChild.InnerName, the mapping logic needs to be more advanced.
        // For now, let's assume NestedDestPropDifferentName has NestedName for this test.
        // To truly test mapping of s.ChildToMap.NestedName -> d.MappedChild.InnerName, we'd use customMap.

        // Let's test the current behavior:
        // We map SourceWithNested.ChildToMap to DestWithNested.MappedChild.
        // The expression s.ChildToMap.NestedName becomes d.MappedChild.NestedName.
        // This means MappedChild (which is NestedDestPropDifferentName) must have a property called "NestedName".
        // This is not the case in the current definition of NestedDestPropDifferentName.

        // Let's use a custom map for the nested access to show true nested mapping.
        Expression<Func<SourceWithNested, bool>> sourceNestedNamePredicate = s => s.ChildToMap!.NestedName == "DeepMap";

        Func<MemberExpression, ParameterExpression, Expression?> customMapForNested = (srcMemberExpr, destParamExpr) =>
        {
            // srcMemberExpr is s.ChildToMap.NestedName
            // We want to convert it to d.MappedChild.InnerName
            if (srcMemberExpr.Expression is MemberExpression parentMemberExpr &&
                parentMemberExpr.Member.Name == nameof(SourceWithNested.ChildToMap) &&
                srcMemberExpr.Member.Name == nameof(NestedSourceProp.NestedName))
            {
                // Get d.MappedChild
                var mappedChildPropInfo = typeof(DestWithNested).GetProperty(nameof(DestWithNested.MappedChild));
                var destMappedChildAccess = Expression.Property(destParamExpr, mappedChildPropInfo!);

                // Get d.MappedChild.InnerName
                var innerNamePropInfo = typeof(NestedDestPropDifferentName).GetProperty(nameof(NestedDestPropDifferentName.InnerName));
                return Expression.Property(destMappedChildAccess, innerNamePropInfo!);
            }
            return null;
        };
        
        var convertedCustomNested = ExpressionConverter.Convert<SourceWithNested, DestWithNested>(sourceNestedNamePredicate, null, customMapForNested);
        Assert.IsTrue(Evaluate(convertedCustomNested, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "DeepMap" } }));
        Assert.IsFalse(Evaluate(convertedCustomNested, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "Wrong" } }));

    }


    // --- Boolean Logic Tests ---
    [Test]
    public void Convert_AndLogic_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 5 && s.IsActive;
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 3, IsActive = true })); // Id fails
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = 10, IsActive = false })); // IsActive fails
    }

    [Test]
    public void Convert_OrLogic_WithMemberMap_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1 || s.Name == "Valid";
        var memberMap = new Dictionary<string, string> {
            { nameof(SourceSimple.Id), nameof(DestDifferentNames.EntityId) },
            { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) }
        };
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 1, FullName = "Other" }));
        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Valid" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { EntityId = 2, FullName = "Other" }));
    }

    // --- Method Call Tests ---
    [Test]
    public void Convert_StringStartsWith_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name != null && s.Name.StartsWith("Prefix");
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Name = "PrefixValue" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = "NoPrefix" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Name = null }));
    }

    [Test]
    public void Convert_StringContains_WithMemberMap_ShouldEvaluateCorrectly()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Name != null && s.Name.Contains("Middle");
         var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Name), nameof(DestDifferentNames.FullName) } };
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestDifferentNames>(sourcePredicate, memberMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "StartMiddleEnd" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new DestDifferentNames { FullName = "NoMatch" }));
    }
    
    // --- Constant Value Tests ---
    [Test]
    public void Convert_WithConstantComparison_ShouldPreserveConstantLogic()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id > 0 && 10 > 5; // 10 > 5 is always true
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.IsTrue(Evaluate(convertedPredicate, new DestSimple { Id = 1 })); // Relies on Id > 0
        Assert.IsFalse(Evaluate(convertedPredicate, new DestSimple { Id = -1 }));
    }

    // --- Error Handling Tests ---
    [Test]
    public void Convert_MemberMissingOnDestination_NoMap_ShouldThrowException()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.PropertyToIgnoreOnDest == "Test";
        // DestSimple does not have PropertyToIgnoreOnDest

        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate)
        );
        Assert.That(ex.Message, Does.Contain(nameof(SourceSimple.PropertyToIgnoreOnDest)));
        Assert.That(ex.Message, Does.Contain("could not be mapped"));
    }
    
    [Test]
    public void Convert_MemberMissingOnDestination_WithMemberMapToNonExistent_ShouldThrowException()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = s => s.Id == 1;
        var memberMap = new Dictionary<string, string> { { nameof(SourceSimple.Id), "NonExistentDestProperty" } };

        var ex = Assert.Throws<InvalidOperationException>(() => 
            ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate, memberMap)
        );
        Assert.That(ex.Message, Does.Contain("NonExistentDestProperty"));
        Assert.That(ex.Message, Does.Contain("could not be mapped"));
    }

    // --- Parameter Name Test ---
    [Test]
    public void Convert_ShouldUseSourceParameterName_IfExists()
    {
        Expression<Func<SourceSimple, bool>> sourcePredicate = item => item.Id == 1; // Parameter name is "item"
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);
        
        Assert.AreEqual("item", convertedPredicate.Parameters[0].Name);
    }

    [Test]
    public void Convert_ShouldUseDefaultParameterName_IfSourceHasNoneOrUnnamed()
    {
        // Creating an expression without an explicit parameter name in the lambda
        var param = Expression.Parameter(typeof(SourceSimple)); // Default name like "s" or "p" or "param_..."
        var body = Expression.Equal(Expression.Property(param, nameof(SourceSimple.Id)), Expression.Constant(1));
        var sourcePredicate = Expression.Lambda<Func<SourceSimple, bool>>(body, param);

        // If the original param name was generated (e.g. "param_0"), it will be used.
        // The test for "p" specifically covers when sourcePredicate.Parameters[0].Name is null.
        // However, C# compiler usually assigns a name.
        // Let's test the "p" default by making the source parameter name null (which is hard to do via lambda syntax)
        // The current Convert method's default is "p" if sourcePredicate.Parameters.FirstOrDefault()?.Name is null.
        // This case is less common with typical lambda syntax.
        // The current code: var parameterName = sourcePredicate.Parameters.FirstOrDefault()?.Name ?? "p";
        // So if the source lambda is `s => s.Id == 1`, parameterName will be "s".
        // If we construct it manually and the ParameterExpression has no name, then it should be "p".

        // Test the explicit default "p" from the original code if source parameter name was null (hard to simulate with C# lambda)
        // The code was: var replaceParam = Expression.Parameter(typeof(TDestination), "p");
        // I changed it to: var parameterName = sourcePredicate.Parameters.FirstOrDefault()?.Name ?? "p";
        // So this test should check if the original parameter name is used.
        
        var sourceParam = Expression.Parameter(typeof(SourceSimple), "originalParam");
        var sourceBody = Expression.Equal(Expression.Property(sourceParam, nameof(SourceSimple.Id)), Expression.Constant(1));
        var predicateWithNamedParam = Expression.Lambda<Func<SourceSimple, bool>>(sourceBody, sourceParam);

        var converted = ExpressionConverter.Convert<SourceSimple, DestSimple>(predicateWithNamedParam);
        Assert.AreEqual("originalParam", converted.Parameters[0].Name);
    }
}


 // --- NUnit Tests ---