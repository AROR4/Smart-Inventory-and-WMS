using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class RejectTransferDto
    {
        [Required]
        public string Reason { get; set; }
            = string.Empty;
    }
}