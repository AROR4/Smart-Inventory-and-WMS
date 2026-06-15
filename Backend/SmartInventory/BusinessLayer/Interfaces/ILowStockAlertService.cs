using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ILowStockAlertService
    {
        Task CheckAndUpdateAlertAsync(
            int productId,
            int warehouseId);

        Task<PagedResponseDto<LowStockAlertResponseDto>> GetActiveAlertsAsync(
            PaginationParams pagination);
}
}