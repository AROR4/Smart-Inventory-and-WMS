using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IWarehouseTaskService
    {
        Task CreateTaskAsync(
            CreateWarehouseTaskDto request);

        Task StartTaskAsync(
            int taskId);

        Task CompleteTaskAsync(
            int taskId);

        Task<WarehouseTaskResponseDto>
            GetTaskByIdAsync(
                int taskId);

        Task<PagedResponseDto<
            WarehouseTaskResponseDto>>
            GetTasksAsync(
                PaginationParams pagination,
                WarehouseTaskFilterDto filter);

        
    }
}