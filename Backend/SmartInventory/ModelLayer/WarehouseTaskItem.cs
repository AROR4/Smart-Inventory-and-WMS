namespace SmartInventoryManagement.Models
{
    public class WarehouseTaskItem
    {
        public int Id { get; set; }

        public int WarehouseTaskId { get; set; }

        public WarehouseTask WarehouseTask { get; set; }
            = null!;

        public int ProductId { get; set; }

        public Product Product { get; set; }
            = null!;

        public int Quantity { get; set; }
    }
}