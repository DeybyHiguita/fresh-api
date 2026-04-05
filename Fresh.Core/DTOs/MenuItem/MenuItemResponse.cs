using System;
using System.Collections.Generic;

namespace Fresh.Core.DTOs.MenuItem
{
    public class MenuItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal PreparationCost { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImgUrl { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MenuItemVariantResponse> Variants { get; set; } = [];
    }
}
