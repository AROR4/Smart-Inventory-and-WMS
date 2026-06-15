namespace SmartInventoryManagement.Models
{
public class PurchaseOrderItem
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; }

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    public decimal UnitPrice { get; set; }
}
}