namespace  SmartInventoryManagement.Models

{
public class Supplier
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string GSTNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = false;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; }
        = new List<PurchaseOrder>();
    
    public ICollection<User> Users { get; set; }
    = new List<User>();
    
}
}