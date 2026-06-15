using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface ILowStockAlertRepository
        : IRepository<LowStockAlert>
    {
        Task<LowStockAlert?>
            GetActiveAlertAsync(
                int productId,
                int warehouseId);

        Task<IEnumerable<LowStockAlert>>
            GetActiveAlertsAsync();
    }
}