using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<Inventory?> GetInventoryAsync(int warehouseId,int productId);

        Task<IEnumerable<Inventory>>GetInventoryWithDetailsAsync();
        

    }
}