using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IStockMovementService
    {
        Task<PagedResponseDto< StockMovementResponseDto>>GetStockMovementsAsync(PaginationParams pagination,StockMovementFilterDto filter);
    }
}