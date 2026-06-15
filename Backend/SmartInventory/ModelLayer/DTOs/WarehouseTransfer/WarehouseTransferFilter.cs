using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class WarehouseTransferFilterDto
    {
        public TransferStatus? Status { get; set; }

        public int? SourceWarehouseId { get; set; }

        public int? DestinationWarehouseId { get; set; }
    }
}