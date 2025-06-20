using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models;
using CrossTypeExpressionConverter;

namespace CrossTypeExpressionConverter.Tests.Units;

[TestFixture]
public class ExpressionConverterV2Tests
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
                    
                var doubledValueProp = typeof(DestForCustomMap).GetProperty(nameof(DestForCustomMap.DoubledValue));
                if (doubledValueProp == null) throw new NullReferenceException("DoubledValue property not found");
                                        
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

    /// <summary>
    /// Tests the conversion of nested properties using a custom map to handle deep mapping scenarios.
    /// </summary>
    [Test]
    public void Convert_NestedProperty_WithCustomMapForDeepMapping_ShouldEvaluateCorrectly()
    {
        // Source: s.ChildToMap.NestedName == "DeepMap"
        // Dest: d.MappedChild.InnerName == "DeepMap"
        // This test demonstrates using customMap to handle mapping of nested properties
        // where both the parent object property and the child's property name might differ.

        Expression<Func<SourceWithNested, bool>> sourceNestedNamePredicate = s => s.ChildToMap!.NestedName == "DeepMap";

         Func<MemberExpression, ParameterExpression, Expression?> customMapForNested = (srcMemberExpr, destParamExpr) =>
        {
            // Check if the expression is s.ChildToMap.NestedName
            if (srcMemberExpr.Expression is MemberExpression parentMemberExpr && // s.ChildToMap [cite: 41]
                parentMemberExpr.Member.Name == nameof(SourceWithNested.ChildToMap) &&
                srcMemberExpr.Member.Name == nameof(NestedSourceProp.NestedName)) // .NestedName [cite: 41, 42]
            {
                // Construct the destination path: d.MappedChild
                var mappedChildPropInfo = typeof(DestWithNested).GetProperty(nameof(DestWithNested.MappedChild)); // [cite: 42]
                if (mappedChildPropInfo == null) throw new NullReferenceException($"{nameof(DestWithNested.MappedChild)} not found."); // [cite: 43]
                var destMappedChildAccess = Expression.Property(destParamExpr, mappedChildPropInfo); // [cite: 43]

                // Construct the final path: d.MappedChild.InnerName
                var innerNamePropInfo = typeof(NestedDestPropDifferentName).GetProperty(nameof(NestedDestPropDifferentName.InnerName)); // [cite: 44]
                if (innerNamePropInfo == null) throw new NullReferenceException($"{nameof(NestedDestPropDifferentName.InnerName)} not found."); // [cite: 45]
                // return Expression.Property(destMappedChildAccess, innerNamePropInfo); // [cite: 45]
                // New return with null check:
                // This creates an expression equivalent to:
                // d.MappedChild == null ? null : d.MappedChild.InnerName
                // So if d.MappedChild is null, the expression part representing s.ChildToMap.NestedName evaluates to null.
                // Then, (null == "DeepMap") becomes false, and no NullReferenceException is thrown.
                return Expression.Condition(
                    Expression.Equal(destMappedChildAccess, Expression.Constant(null, mappedChildPropInfo.PropertyType)), // Condition: d.MappedChild == null
                    Expression.Constant(null, innerNamePropInfo.PropertyType), // If true: evaluate to null (for InnerName)
                    Expression.Property(destMappedChildAccess, innerNamePropInfo)  // If false: evaluate to d.MappedChild.InnerName
                );
            }
            return null; // Fallback for other members [cite: 46]
        };
            
        var convertedCustomNested = ExpressionConverter.Convert<SourceWithNested, DestWithNested>(sourceNestedNamePredicate, null, customMapForNested);
        Assert.IsTrue(Evaluate(convertedCustomNested, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "DeepMap" } }));
        Assert.IsFalse(Evaluate(convertedCustomNested, new DestWithNested { MappedChild = new NestedDestPropDifferentName { InnerName = "Wrong" } }));
        Assert.IsFalse(Evaluate(convertedCustomNested, new DestWithNested { MappedChild = null })); // Should be false if MappedChild is null
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

    /// <summary>
    /// Tests if the converter uses the default parameter name "p" when the source parameter has no name.
    /// </summary>
    [Test]
    public void Convert_ShouldUseDefaultParameterName_IfSourceParameterHasNoName()
    {
        // Creating an expression where the C# compiler might generate a less predictable name,
        // or if constructed manually without a name.
        var param = Expression.Parameter(typeof(SourceSimple)); 
        var body = Expression.Equal(Expression.Property(param, nameof(SourceSimple.Id)), Expression.Constant(1));
        var sourcePredicate = Expression.Lambda<Func<SourceSimple, bool>>(body, param);

        // The converter defaults to "p" if the source parameter name is null.
        // However, Expression.Parameter(type) usually assigns a name (like "param_0") if not specified.
        // To truly test the "?? 'p'" fallback, the sourcePredicate.Parameters.FirstOrDefault()?.Name would need to be null.
        // This is hard to achieve with standard C# lambda or Expression.Parameter.
        // The current behavior is that it WILL use the compiler-generated name if one exists.
        // If for some reason param.Name was null, it would default to "p".
        // For this test, we assert it uses the name from the ParameterExpression, whatever it is.
        var expectedNameInConvertedExpression = param.Name ?? "p";
        var convertedPredicate = ExpressionConverter.Convert<SourceSimple, DestSimple>(sourcePredicate);

        Assert.That(convertedPredicate.Parameters[0].Name, Is.EqualTo(expectedNameInConvertedExpression));


        // To specifically test the "p" fallback, we'd need a way for sourcePredicate.Parameters.FirstOrDefault()?.Name to be null.
        // This is not straightforward. The more common scenario is a name exists.
    }

    // --- Tests for User's Scenarios (NewsDataModel to ArticleDataModel) ---
    [Test]
    public void Convert_NewsToArticle_WithComprehensiveMemberMap_TitleMatch_ShouldEvaluateCorrectly()
    {
        // Simulating the map that would be generated by MappingUtils.BuildMemberMap from user's example
        var newsToArticleMap = new Dictionary<string, string>
        {
            { nameof(NewsDataModel.UserIdOwner), nameof(ArticleDataModel.OwnerId) },
            { nameof(NewsDataModel.NewsTitle), nameof(ArticleDataModel.Title) },
            { nameof(NewsDataModel.NewsContent), nameof(ArticleDataModel.Content) },
            { nameof(NewsDataModel.NewsPermission), nameof(ArticleDataModel.Permission) },
            { nameof(NewsDataModel.NewsChangedById), nameof(ArticleDataModel.ChangedById) },
            { nameof(NewsDataModel.CategoryId), nameof(ArticleDataModel.CategoryId) }, // Same name
            { nameof(NewsDataModel.PublicationType), nameof(ArticleDataModel.PublicationType) }, // Same name
            { nameof(NewsDataModel.GalleryId), nameof(ArticleDataModel.GalleryId) }, // Same name
            { nameof(NewsDataModel.NewsPresentation), nameof(ArticleDataModel.Presentation) },
            { nameof(NewsDataModel.PublicationDate), nameof(ArticleDataModel.PublicationDate) }, // Same name
            { nameof(NewsDataModel.TitleForUrl), nameof(ArticleDataModel.TitleForUrl) }, // Same name
            { nameof(NewsDataModel.HashtagsNewsId), nameof(ArticleDataModel.HashtagsArticleId) },
            { nameof(NewsDataModel.ArticleVersion), nameof(ArticleDataModel.ArticleVersion) }, // Same name
            { nameof(NewsDataModel.Id), nameof(ArticleDataModel.Id) }, // Same name
            { nameof(NewsDataModel.CreatedDate), nameof(ArticleDataModel.CreatedDate) }, // Same name
            { nameof(NewsDataModel.UpdatedDate), nameof(ArticleDataModel.UpdatedDate) } // Same name
        };

        Expression<Func<NewsDataModel, bool>> sourcePredicate = news => news.NewsTitle == "Breaking News";
        var convertedPredicate = ExpressionConverter.Convert<NewsDataModel, ArticleDataModel>(sourcePredicate, newsToArticleMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new ArticleDataModel { Title = "Breaking News" }));
        Assert.IsFalse(Evaluate(convertedPredicate, new ArticleDataModel { Title = "Old News" }));
    }

    [Test]
    public void Convert_NewsToArticle_WithComprehensiveMemberMap_DateComparison_ShouldEvaluateCorrectly()
    {
        var newsToArticleMap = new Dictionary<string, string> // Same map as above
        {
            { nameof(NewsDataModel.UserIdOwner), nameof(ArticleDataModel.OwnerId) },
            { nameof(NewsDataModel.NewsTitle), nameof(ArticleDataModel.Title) },
            { nameof(NewsDataModel.NewsContent), nameof(ArticleDataModel.Content) },
            { nameof(NewsDataModel.NewsPermission), nameof(ArticleDataModel.Permission) },
            { nameof(NewsDataModel.NewsChangedById), nameof(ArticleDataModel.ChangedById) },
            { nameof(NewsDataModel.CategoryId), nameof(ArticleDataModel.CategoryId) },
            { nameof(NewsDataModel.PublicationType), nameof(ArticleDataModel.PublicationType) },
            { nameof(NewsDataModel.GalleryId), nameof(ArticleDataModel.GalleryId) },
            { nameof(NewsDataModel.NewsPresentation), nameof(ArticleDataModel.Presentation) },
            { nameof(NewsDataModel.PublicationDate), nameof(ArticleDataModel.PublicationDate) },
            { nameof(NewsDataModel.TitleForUrl), nameof(ArticleDataModel.TitleForUrl) },
            { nameof(NewsDataModel.HashtagsNewsId), nameof(ArticleDataModel.HashtagsArticleId) },
            { nameof(NewsDataModel.ArticleVersion), nameof(ArticleDataModel.ArticleVersion) },
            { nameof(NewsDataModel.Id), nameof(ArticleDataModel.Id) },
            { nameof(NewsDataModel.CreatedDate), nameof(ArticleDataModel.CreatedDate) },
            { nameof(NewsDataModel.UpdatedDate), nameof(ArticleDataModel.UpdatedDate) }
        };
        var testDate = new DateTime(2023, 1, 15);
        Expression<Func<NewsDataModel, bool>> sourcePredicate = news => news.PublicationDate > testDate;
        var convertedPredicate = ExpressionConverter.Convert<NewsDataModel, ArticleDataModel>(sourcePredicate, newsToArticleMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new ArticleDataModel { PublicationDate = testDate.AddDays(1) }));
        Assert.IsFalse(Evaluate(convertedPredicate, new ArticleDataModel { PublicationDate = testDate.AddDays(-1) }));
    }

    // --- Test for "Find" like scenario (SubjectDatabaseModel to SubjectsDatamodel) ---
    [Test]
    public void Convert_SubjectToDatamodel_NameAndMandatory_WithMemberMap_ShouldEvaluateCorrectly()
    {
        var subjectMap = new Dictionary<string, string>
        {
            // SubjectKey is same name, no map needed unless explicit override
            { nameof(SubjectDatabaseModel.SubjectName), nameof(SubjectsDatamodel.NameOfSubject) },
            { nameof(SubjectDatabaseModel.IsMandatory), nameof(SubjectsDatamodel.Mandatory) }
        };

        Expression<Func<SubjectDatabaseModel, bool>> sourcePredicate = 
            subject => subject.SubjectName.Contains("Math") && subject.IsMandatory;
            
        var convertedPredicate = ExpressionConverter.Convert<SubjectDatabaseModel, SubjectsDatamodel>(sourcePredicate, subjectMap);

        Assert.IsTrue(Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "Advanced Mathematics", Mandatory = true }));
        Assert.IsFalse(Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "History", Mandatory = true })); // Name fails
        Assert.IsFalse(Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "Basic Mathematics", Mandatory = false })); // Mandatory fails
    }
}