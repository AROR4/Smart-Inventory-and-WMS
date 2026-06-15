using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class LowStockAlertRepository
        : Repository<LowStockAlert>,
          ILowStockAlertRepository
    {
        private readonly ApplicationDbContext _context;

        public LowStockAlertRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task<LowStockAlert?>
            GetActiveAlertAsync(
                int productId,
                int warehouseId)
        {
            return await _context
                .LowStockAlerts
                .FirstOrDefaultAsync(
                    a =>
                        a.ProductId == productId
                        &&
                        a.WarehouseId == warehouseId
                        &&
                        !a.IsResolved);
        }

        public async Task<IEnumerable<LowStockAlert>>
            GetActiveAlertsAsync()
        {
            return await _context
                .LowStockAlerts
                .Include(a => a.Product)
                .Include(a => a.Warehouse)
                .Where(a => !a.IsResolved)
                .OrderByDescending(
                    a => a.CreatedAt)
                .ToListAsync();
        }
    }
}