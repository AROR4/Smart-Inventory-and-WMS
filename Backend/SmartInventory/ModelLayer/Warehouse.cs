using SmartInventoryManagement.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models
{
    public class Warehouse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public StorageType StorageType { get; set; }

        public decimal Capacity { get; set; }

        public decimal AvailableCapacity { get; set; }

        public decimal ReservedCapacity { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        public ICollection<WarehouseTransfer> SourceTransfers { get; set; } = new List<WarehouseTransfer>();

        public ICollection<WarehouseTransfer> DestinationTransfers { get; set; } = new List<WarehouseTransfer>();

        public ICollection<LowStockAlert> LowStockAlerts { get; set; } = new List<LowStockAlert>();

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}