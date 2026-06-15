namespace SmartInventoryManagement.Models.DTOs
{
    public class WarehouseTransferResponseDto
    {
        public int Id { get; set; }

        public string TransferNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string SourceWarehouse { get; set; } = string.Empty;

        public string DestinationWarehouse { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public decimal TransferVolume { get; set; }

        public string? Reason { get; set; }

        public DateTime TransferDate { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime? CompletedDate { get; set; }

        public List<WarehouseTransferItemResponseDto> Items { get; set; } = new();
    }
}