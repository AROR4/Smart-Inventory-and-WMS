using System.ComponentModel.DataAnnotations;
using SmartInventoryManagement.Models.Enums;

public class UpdateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Length { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Width { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Height { get; set; }

    [Range(1, int.MaxValue)]
    public int UnitsPerCarton { get; set; } = 1;

    [Required]
    public StorageType RequiredStorageType { get; set; }
}