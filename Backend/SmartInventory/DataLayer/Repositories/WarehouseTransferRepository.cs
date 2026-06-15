using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class WarehouseTransferRepository
        : Repository<WarehouseTransfer>,
          IWarehouseTransferRepository
    {
        private readonly ApplicationDbContext _context;

        public WarehouseTransferRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task<WarehouseTransfer?>
            GetTransferWithDetailsAsync(
                int id)
        {
            return await _context
                .WarehouseTransfers
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.CreatedByUser)
                .Include(t => t.WarehouseTransferItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(
                    t => t.Id == id);
        }

        public async Task<IEnumerable<WarehouseTransfer>>
            GetTransfersWithDetailsAsync()
        {
            return await _context
                .WarehouseTransfers
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.CreatedByUser)
                .Include(t => t.WarehouseTransferItems)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(
                    t => t.TransferDate)
                .ToListAsync();
        }

        public async Task<WarehouseTransfer?>
            GetByTransferNumberAsync(string transferNumber)
        {
            return await _context
                .WarehouseTransfers
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.CreatedByUser)
                .Include(t => t.WarehouseTransferItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(
                    t => t.TransferNumber ==
                        transferNumber);
        }
    }
}