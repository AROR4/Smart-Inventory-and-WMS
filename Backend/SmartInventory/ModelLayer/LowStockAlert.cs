namespace SmartInventoryManagement.Models
{
public class LowStockAlert
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; }=null!;

    public int WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; }=null!;

    public int CurrentQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
}