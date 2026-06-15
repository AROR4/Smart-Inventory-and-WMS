using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class AdjustStockDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Range(0, int.MaxValue)]
        public int NewQuantity { get; set; }

        public string Reason { get; set; }
            = string.Empty;
    }
}