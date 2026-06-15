namespace SmartInventoryManagement.Models.DTOs
{
    public class WarehouseTaskItemResponseDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public int Quantity { get; set; }
    }
}