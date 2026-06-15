using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public int WarehouseId { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public int Quantity { get; set; }

        public StockMovementType Type { get; set; }

        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int CreatedByUserId { get; set; }

        public User CreatedByUser { get; set; } = null!;
    }
}