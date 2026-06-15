using SmartInventoryManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models
{
public class WarehouseTransfer
{
    public int Id { get; set; }
    public string TransferNumber { get; set; }=String.Empty;
    public int SourceWarehouseId { get; set; }

    public Warehouse SourceWarehouse { get; set; }

    public int DestinationWarehouseId { get; set; }

    public Warehouse DestinationWarehouse { get; set; }

    public int CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; }

    public decimal TransferVolume { get; set; }

    public string? Reason { get; set; }

    public TransferStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.Now;

    public DateTime? CompletedDate { get; set; }

    public ICollection<WarehouseTransferItem> WarehouseTransferItems { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
}