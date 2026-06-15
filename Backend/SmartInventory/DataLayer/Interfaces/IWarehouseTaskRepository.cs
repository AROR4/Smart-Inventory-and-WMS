using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IWarehouseTaskRepository
        : IRepository<WarehouseTask>
    {
        Task<WarehouseTask?>
            GetTaskWithDetailsAsync(
                int taskId);

        Task<IEnumerable<WarehouseTask>>
            GetTasksWithDetailsAsync();

        Task<WarehouseTask?> GetTaskByReferenceAsync(
            WarehouseTaskReferenceType referenceType,
            int referenceId,
            int warehouseId,
            WarehouseTaskType taskType);
    }
}