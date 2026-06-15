using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class CreatePurchaseOrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}