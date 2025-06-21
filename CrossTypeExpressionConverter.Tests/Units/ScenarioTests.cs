using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains tests for real-world conversion scenarios involving more complex data models.
/// </summary>
[TestFixture]
public class ScenarioTests
{
    /// <summary>
    /// Compiles and executes a predicate against an item, returning the boolean result.
    /// </summary>


    /// <summary>
    /// Verifies conversion of a predicate on a string property between two complex, real-world-like models.
    /// </summary>
    [Test]
    public void Convert_NewsToArticle_WithComprehensiveMemberMap_TitleMatch_ShouldEvaluateCorrectly()
    {
        // Arrange
        var newsToArticleMap = new Dictionary<string, string>
        {
            { nameof(NewsDataModel.UserIdOwner), nameof(ArticleDataModel.OwnerId) },
            { nameof(NewsDataModel.NewsTitle), nameof(ArticleDataModel.Title) },
            { nameof(NewsDataModel.NewsContent), nameof(ArticleDataModel.Content) },
            // ... other mappings would be here ...
        };
        var options = new ExpressionConverterOptions().WithMemberMap(newsToArticleMap);
        Expression<Func<NewsDataModel, bool>> sourcePredicate = news => news.NewsTitle == "Breaking News";

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<NewsDataModel, ArticleDataModel>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new ArticleDataModel { Title = "Breaking News" }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new ArticleDataModel { Title = "Old News" }), Is.False);
    }

    /// <summary>
    /// Verifies conversion of a predicate with a date comparison between two complex models.
    /// </summary>
    [Test]
    public void Convert_NewsToArticle_WithComprehensiveMemberMap_DateComparison_ShouldEvaluateCorrectly()
    {
        // Arrange
        var newsToArticleMap = new Dictionary<string, string> 
        {
            { nameof(NewsDataModel.PublicationDate), nameof(ArticleDataModel.PublicationDate) },
             // ... other mappings would be here ...
        };
        var options = new ExpressionConverterOptions().WithMemberMap(newsToArticleMap);
        var testDate = new DateTime(2023, 1, 15);
        Expression<Func<NewsDataModel, bool>> sourcePredicate = news => news.PublicationDate > testDate;

        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<NewsDataModel, ArticleDataModel>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new ArticleDataModel { PublicationDate = testDate.AddDays(1) }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new ArticleDataModel { PublicationDate = testDate.AddDays(-1) }), Is.False);
    }

    /// <summary>
    /// Verifies conversion for a "find" like scenario, involving a predicate with both a string method call and a boolean check.
    /// </summary>
    [Test]
    public void Convert_SubjectToDatamodel_NameAndMandatory_WithMemberMap_ShouldEvaluateCorrectly()
    {
        // Arrange
        var subjectMap = new Dictionary<string, string>
        {
            { nameof(SubjectDatabaseModel.SubjectName), nameof(SubjectsDatamodel.NameOfSubject) },
            { nameof(SubjectDatabaseModel.IsMandatory), nameof(SubjectsDatamodel.Mandatory) }
        };
        var options = new ExpressionConverterOptions().WithMemberMap(subjectMap);
        Expression<Func<SubjectDatabaseModel, bool>> sourcePredicate = 
            subject => subject.SubjectName.Contains("Math") && subject.IsMandatory;
            
        // Act
        var convertedPredicate = ExpressionConverterFacade.Convert<SubjectDatabaseModel, SubjectsDatamodel>(sourcePredicate, options);

        // Assert
        Assert.That(TestUtils.Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "Advanced Mathematics", Mandatory = true }), Is.True);
        Assert.That(TestUtils.Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "History", Mandatory = true }), Is.False, "Should fail on name mismatch.");
        Assert.That(TestUtils.Evaluate(convertedPredicate, new SubjectsDatamodel { NameOfSubject = "Basic Mathematics", Mandatory = false }), Is.False, "Should fail on mandatory flag mismatch.");
    }
}
