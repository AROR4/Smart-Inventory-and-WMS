namespace SmartInventoryManagement.Models.DTOs
{
    public class LowStockAlertResponseDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public string SKU { get; set; }
            = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; }
            = string.Empty;

        public int CurrentQuantity { get; set; }

        public int ReorderLevel { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}