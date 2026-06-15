using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IWarehouseTransferService
    {
        Task CreateTransferAsync(
            CreateWarehouseTransferDto request);

        Task ApproveTransferAsync(
            int id);

        Task MarkReceivedAsync(
            int id);

        Task CompleteTransferAsync(
            int id);

        Task<WarehouseTransferResponseDto>
            GetTransferByIdAsync(
                int id);

        Task<WarehouseTransferResponseDto> GetByTransferNumberAsync( string transferNumber);

        Task RejectTransferAsync(int id,string reason);

        Task MarkInTransitAsync(int id);

        Task CancelTransferAsync(int id);

        Task<PagedResponseDto<
            WarehouseTransferResponseDto>>
            GetTransfersAsync(
                PaginationParams pagination,
                WarehouseTransferFilterDto filter);
        


    }
}