namespace SmartInventoryManagement.Models.DTOs
{
    public class PurchaseOrderResponseDto
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; }
            = string.Empty;

        public string SupplierName { get; set; }
            = string.Empty;

        public string WarehouseName { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public decimal TotalVolume { get; set; }

        public string? InvoiceNumber { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime OrderedDate { get; set; }

        public DateTime? ReceivedDate { get; set; }

        public string CreatedBy { get; set; }
            = string.Empty;

        public List<PurchaseOrderItemResponseDto>
            Items { get; set; } = new();
    }
}