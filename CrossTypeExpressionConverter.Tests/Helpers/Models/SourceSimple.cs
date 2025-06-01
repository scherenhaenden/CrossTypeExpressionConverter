namespace CrossTypeExpressionConverter.Tests.Helpers.Models;

 // --- Test Model Classes ---
    public class SourceSimple
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public double Value { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PropertyToIgnoreOnDest { get; set; }
    }

    public class DestSimple
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public double Value { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DestDifferentNames
    {
        public int EntityId { get; set; }
        public string? FullName { get; set; }
        public bool Enabled { get; set; }
        public double NumericValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class NestedSourceProp
    {
        public int NestedId { get; set; }
        public string? NestedName { get; set; }
    }

    public class SourceWithNested
    {
        public int ParentId { get; set; }
        public NestedSourceProp? Child { get; set; }
        public NestedSourceProp? ChildToMap { get; set; }
    }

    public class NestedDestProp
    {
        public int NestedId { get; set; }
        public string? NestedName { get; set; }
    }
    
    public class NestedDestPropDifferentName
    {
        public int InnerId {get; set;} // Mapped from NestedId
        public string? InnerName {get; set;} // Mapped from NestedName
    }

    public class DestWithNested
    {
        public int ParentId { get; set; }
        public NestedDestProp? Child { get; set; } // Same name for nested object property
        public NestedDestPropDifferentName? MappedChild { get; set; } // Different name for nested object property
    }

    public class SourceForCustomMap
    {
        public int Id { get; set; }
        public string? Data { get; set; }
        public int NumericValue { get; set; }
    }

    public class DestForCustomMap
    {
        public int Id { get; set; }
        public string? TransformedData { get; set; } // Data will be mapped here by custom logic
        public int DoubledValue { get; set; } // NumericValue will be mapped and transformed here
    }

    // --- Models for User's Scenarios ---
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

    public class SubjectDatabaseModel // Source for "Find" like scenario
    {
        public int SubjectKey { get; set; }
        public string? SubjectName { get; set; }
        public bool IsMandatory { get; set; }
    }

    public class SubjectsDatamodel // Destination for "Find" like scenario
    {
        public int SubjectKey { get; set; } // Same name
        public string? NameOfSubject { get; set; } // Different name
        public bool Mandatory { get; set; } // Different name
    }

