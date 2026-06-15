using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task CreatePurchaseOrderAsync(
            CreatePurchaseOrderDto request);

        Task ApprovePurchaseOrderAsync(
            int id);

        Task RejectPurchaseOrderAsync(
            int id,
            string reason);

        Task ReceivePurchaseOrderAsync(
            int id,
            string invoiceNumber);

        Task<PurchaseOrderResponseDto>
            GetPurchaseOrderByIdAsync(
                int id);

        Task<PurchaseOrderResponseDto>
            GetByOrderNumberAsync(
                string orderNumber);
        
        Task CompletePurchaseOrderAsync(int id);

        Task<PagedResponseDto<
                PurchaseOrderResponseDto>>
            GetPurchaseOrdersAsync(
                PaginationParams pagination,
                PurchaseOrderFilterDto filter);
            }
}