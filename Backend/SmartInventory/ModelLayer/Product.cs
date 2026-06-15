using System.ComponentModel.DataAnnotations.Schema;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int CompanyId { get; set; }

        public string? Description { get; set; }

        public string Barcode { get; set; } = string.Empty;

        public string? ModelNumber { get; set; }

        public decimal UnitPrice { get; set; }

        public int ReorderLevel { get; set; }

        public StorageType RequiredStorageType { get; set; }

        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }

        public decimal Length { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        [NotMapped]
        public decimal Volume =>Length * Width * Height;

        public int UnitsPerCarton { get; set; } = 1;

        public Category Category { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

        public ICollection<WarehouseTransferItem> WarehouseTransferItems { get; set; } = new List<WarehouseTransferItem>();

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<LowStockAlert>LowStockAlerts { get; set; } = new List<LowStockAlert>();
    }
}