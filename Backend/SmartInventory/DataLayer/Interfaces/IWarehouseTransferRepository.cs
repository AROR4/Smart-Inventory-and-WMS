using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IWarehouseTransferRepository : IRepository<WarehouseTransfer>
    {
        Task<WarehouseTransfer?> GetTransferWithDetailsAsync(int id);

        Task<IEnumerable<WarehouseTransfer>> GetTransfersWithDetailsAsync();

        Task<WarehouseTransfer?> GetByTransferNumberAsync(string transferNumber);
    }
}