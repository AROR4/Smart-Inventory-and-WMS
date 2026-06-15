using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class ProductFilterDto
    {
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public int? CompanyId { get; set; }

        public StorageType? StorageType { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }
    }
}