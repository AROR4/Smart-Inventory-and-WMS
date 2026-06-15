using System.ComponentModel.DataAnnotations;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreateWarehouseTaskDto
    {
        [Required]
        public WarehouseTaskType Type { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }
            = string.Empty;

        public WarehouseTaskReferenceType? ReferenceType { get; set; }

        [Range(1, int.MaxValue)]
        public int? ReferenceId { get; set; }

        public List<CreateWarehouseTaskItemDto>
            Items { get; set; }
                = new();
            
    }
}