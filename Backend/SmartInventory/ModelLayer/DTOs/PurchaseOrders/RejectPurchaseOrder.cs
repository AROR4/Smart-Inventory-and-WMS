using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class RejectPurchaseOrderDto
    {
        [Required]
        public string Reason { get; set; }
            = string.Empty;
    }
}