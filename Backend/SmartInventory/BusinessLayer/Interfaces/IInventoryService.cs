using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    
public interface IInventoryService
{
    Task AddStockAsync(
        AddStockDto request);

    Task RemoveStockAsync(
        RemoveStockDto request);

    Task AdjustStockAsync(
        AdjustStockDto request);

    Task<PagedResponseDto<InventoryResponseDto>>
        GetInventoryAsync(
            PaginationParams pagination,
            InventoryFilterDto filter);

    Task ExecuteStoreInventoryAsync(
        int productId,
        int warehouseId,
        int quantity,
        string reason);

    Task ExecuteRetrieveInventoryAsync(
        int productId,
        int warehouseId,
        int quantity,
        string reason);
}

}