using SmartInventoryManagement.Models.Enums;

public class WarehouseTaskFilterDto
{
    public WarehouseTaskType? Type { get; set; }

    public int? WarehouseId { get; set; }

    public TaskStatusType? Status { get; set; }
}