using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    
    public class CreateWarehouseTransferDto
    {
        [Required]
        public int SourceWarehouseId { get; set; }

        [Required]
        public int DestinationWarehouseId { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        public List<CreateWarehouseTransferItemDto> Items { get; set; }
            = new();
    }
}