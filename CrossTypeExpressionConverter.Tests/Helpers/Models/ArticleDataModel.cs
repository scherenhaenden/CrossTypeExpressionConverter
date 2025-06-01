namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class ArticleDataModel // Destination
{
    public int OwnerId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int Permission { get; set; }
    public int ChangedById { get; set; }
    public int CategoryId { get; set; } // Same name
    public string? PublicationType { get; set; } // Same name
    public int GalleryId { get; set; } // Same name
    public string? Presentation { get; set; }
    public DateTime PublicationDate { get; set; } // Same name
    public string? TitleForUrl { get; set; } // Same name
    public int HashtagsArticleId { get; set; }
    public string? ArticleVersion { get; set; } // Same name
    public int Id { get; set; } // Same name
    public DateTime CreatedDate { get; set; } // Same name
    public DateTime UpdatedDate { get; set; } // Same name
}