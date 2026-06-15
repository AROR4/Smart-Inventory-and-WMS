namespace SmartInventoryManagement.Models.DTOs
{
    public class PurchaseOrderItemResponseDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public int OrderedQuantity { get; set; }

        public int ReceivedQuantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}