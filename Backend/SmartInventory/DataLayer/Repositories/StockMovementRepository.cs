using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
    {
        public StockMovementRepository(ApplicationDbContext context): base(context)
        {
        }

        public async Task<IEnumerable<StockMovement>>
            GetStockMovementsWithDetailsAsync()
        {
            return await _context.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.CreatedByUser)
                .OrderByDescending(sm => sm.CreatedAt)
                .ToListAsync();
        }


    }
}