namespace SmartInventoryManagement.Models.DTOs
{
    public class WarehouseTaskResponseDto
    {
        public int Id { get; set; }

        public string Type { get; set; }
            = string.Empty;

        public string Description { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; }
            = string.Empty;

        public string CreatedBy { get; set; }
            = string.Empty;

        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }
        
        public string? StartedBy { get; set; }

        public string? CompletedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public List<WarehouseTaskItemResponseDto>
            Items { get; set; }
                = new();
    }
}