using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class PurchaseOrderRepository
        : Repository<PurchaseOrder>,
          IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task<PurchaseOrder?>
            GetPurchaseOrderWithDetailsAsync(int id)
        {
            return await _context
                .PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(
                    po => po.Id == id);
        }

        public async Task<IEnumerable<PurchaseOrder>>
            GetPurchaseOrdersWithDetailsAsync()
        {
            return await _context
                .PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(
                    po => po.OrderedDate)
                .ToListAsync();
        }

        public async Task<PurchaseOrder?>
            GetByOrderNumberAsync(
                string orderNumber)
        {
            return await _context
                .PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(
                    po => po.OrderNumber ==
                        orderNumber);
        }

        public async Task<
            IEnumerable<PurchaseOrder>>
            GetSupplierPendingOrdersAsync(
                int supplierId)
        {
            return await _context
                .PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(po =>
                    po.SupplierId == supplierId
                    &&
                    po.Status ==
                        PurchaseOrderStatus.Ordered)
                .OrderByDescending(
                    po => po.OrderedDate)
                .ToListAsync();
        }
        
        public async Task<
            IEnumerable<PurchaseOrder>>
            GetSupplierOrderHistoryAsync(
                int supplierId)
        {
            return await _context
                .PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedByUser)
                .Include(po => po.PurchaseOrderItems)
                    .ThenInclude(i => i.Product)
                .Where(po =>
                    po.SupplierId == supplierId
                    &&
                    po.Status !=
                        PurchaseOrderStatus.Ordered)
                .OrderByDescending(
                    po => po.OrderedDate)
                .ToListAsync();
        }
    }
}