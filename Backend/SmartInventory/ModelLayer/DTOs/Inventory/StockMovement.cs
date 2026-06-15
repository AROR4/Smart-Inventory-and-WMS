namespace SmartInventoryManagement.Models.DTOs
{
    public class StockMovementResponseDto
    {
        public int Id { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public string WarehouseName { get; set; }
            = string.Empty;

        public int Quantity { get; set; }

        public string Type { get; set; }
            = string.Empty;

        public string Reason { get; set; }
            = string.Empty;

        public string PerformedBy { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}