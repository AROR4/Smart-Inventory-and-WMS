using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class AddStockDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string? Reason { get; set; }
    }
}