using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class ReceivePurchaseOrderDto
    {
        [Required]
        public string InvoiceNumber { get; set; }
            = string.Empty;
    }
}