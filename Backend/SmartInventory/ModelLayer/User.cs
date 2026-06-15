namespace SmartInventoryManagement.Models
{
public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public int? AssignedWarehouseId { get; set; }

    public int? SupplierId { get; set; }

    public bool IsPasswordSet { get; set; }

    public Role Role { get; set; } = null!;

    public Warehouse? AssignedWarehouse { get; set; }

    public Supplier? Supplier { get; set; }

}
}