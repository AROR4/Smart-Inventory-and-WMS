using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreateWarehouseTaskItemDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}