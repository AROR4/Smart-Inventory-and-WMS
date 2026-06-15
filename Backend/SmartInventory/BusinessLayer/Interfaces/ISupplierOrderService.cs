using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ISupplierOrderService
    {
        Task<PagedResponseDto<
            PurchaseOrderResponseDto>>
            GetPendingOrdersAsync(
                PaginationParams pagination);

        Task<PagedResponseDto<
            PurchaseOrderResponseDto>>
            GetOrderHistoryAsync(
                PaginationParams pagination);

        Task<PurchaseOrderResponseDto>
            GetOrderDetailsAsync(
                int id);

        Task MarkOrderShippedAsync(
            int id);
    }   
}