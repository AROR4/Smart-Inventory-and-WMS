namespace SmartInventoryManagement.Models
{
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; }
}
}