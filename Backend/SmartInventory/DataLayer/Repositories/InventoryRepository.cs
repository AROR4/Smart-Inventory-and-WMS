using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(ApplicationDbContext context): base(context)
        {
        }

        public async Task<Inventory?> GetInventoryAsync(
            int warehouseId,
            int productId
            )
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(i =>
                    i.ProductId == productId &&
                    i.WarehouseId == warehouseId);
        }

        public async Task<IEnumerable<Inventory>>
            GetInventoryWithDetailsAsync()
        {
            return await _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Company)
                .Include(i => i.Warehouse)
                .ToListAsync();
        }
        
        


    }
}