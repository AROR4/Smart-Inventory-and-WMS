using System.ComponentModel.DataAnnotations;

namespace SmartInventoryManagement.Models.DTOs
{
    public class UpdateCategoryDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}