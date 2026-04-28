       public record SearchFilter
        {
            //Make sure it matches Album and Articles
            public string? Title { get; set; } = string.Empty;
            public string? Description { get; set; } = string.Empty;
            public string? Author { get; set; } = string.Empty;

            public string? SearchTerm { get; set; } = string.Empty;

            public string? DateFrom { get; set; } = string.Empty;
            public string? DateTo { get; set; } = string.Empty;

            public string? Type { get; set; } = string.Empty;
        }