using System;

namespace Fresh.Core.Entities
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal PreparationCost { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImgUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
