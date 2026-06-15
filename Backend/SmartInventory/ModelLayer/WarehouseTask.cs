using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models
{
    
    public class WarehouseTask
    {
        public int Id { get; set; }

        public WarehouseTaskType Type { get; set; }

        public int WarehouseId { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public string Description { get; set; }
            = string.Empty;

        public TaskStatusType Status { get; set; }

        public int CreatedByUserId { get; set; }

        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public WarehouseTaskReferenceType? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int? StartedByUserId { get; set; }

        public User? StartedByUser { get; set; }

        public DateTime? StartedAt { get; set; }

        public int? CompletedByUserId { get; set; }

        public User? CompletedByUser { get; set; }

        public DateTime? CompletedAt { get; set; }

        public ICollection<WarehouseTaskItem>
            WarehouseTaskItems { get; set; }
                = new List<WarehouseTaskItem>();
    }

}