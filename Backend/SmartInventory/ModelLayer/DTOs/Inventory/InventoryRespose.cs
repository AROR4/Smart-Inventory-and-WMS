namespace SmartInventoryManagement.Models.DTOs
{
    public class InventoryResponseDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public string CompanyName { get; set; }
            = string.Empty;

        public string WarehouseName { get; set; }
            = string.Empty;

        public int Quantity { get; set; }

        public decimal OccupiedVolume { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}