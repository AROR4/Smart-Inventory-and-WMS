using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IStockMovementRepository : IRepository<StockMovement>
    {
        Task<IEnumerable<StockMovement>> GetStockMovementsWithDetailsAsync();
    }
}