using SmartInventoryManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models
{
public class PurchaseOrder
{
    public int Id { get; set; }

    public string OrderNumber { get; set; }=String.Empty;

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; }

    public int CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; }

    public int WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; }

    public decimal TotalVolume { get; set; }

    public DateTime OrderedDate { get; set; } = DateTime.Now;

    public string? RejectionReason { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
}