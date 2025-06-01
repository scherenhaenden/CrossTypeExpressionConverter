namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

public class NewsDataModel // Source
{
    public int UserIdOwner { get; set; }
    public string? NewsTitle { get; set; }
    public string? NewsContent { get; set; }
    public int NewsPermission { get; set; } // Assuming int, adjust if different
    public int NewsChangedById { get; set; }
    public int CategoryId { get; set; }
    public string? PublicationType { get; set; } // Assuming string
    public int GalleryId { get; set; }
    public string? NewsPresentation { get; set; } // Assuming string
    public DateTime PublicationDate { get; set; }
    public string? TitleForUrl { get; set; }
    public int HashtagsNewsId { get; set; }
    public string? ArticleVersion { get; set; } // Assuming string
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}