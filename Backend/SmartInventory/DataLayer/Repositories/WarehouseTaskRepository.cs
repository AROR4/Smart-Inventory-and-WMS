using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Enums;

namespace SmartInventoryManagement.DataLayer.Repositories
{
    public class WarehouseTaskRepository
        : Repository<WarehouseTask>,
          IWarehouseTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public WarehouseTaskRepository(
            ApplicationDbContext context)
            : base(context)
        {
            _context = context;
        }

        public async Task<WarehouseTask?>
            GetTaskWithDetailsAsync(
                int taskId)
        {
            return await _context
                .WarehouseTasks
                .Include(t => t.Warehouse)
                .Include(t => t.CreatedByUser)
                .Include(t => t.StartedByUser)
                .Include(t => t.CompletedByUser)
                .Include(t => t.WarehouseTaskItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(
                    t => t.Id == taskId);
        }

        public async Task<IEnumerable<WarehouseTask>>
            GetTasksWithDetailsAsync()
        {
            return await _context
                .WarehouseTasks
                .Include(t => t.Warehouse)
                .Include(t => t.CreatedByUser)
                .Include(t => t.StartedByUser)
                .Include(t => t.CompletedByUser)
                .Include(t => t.WarehouseTaskItems)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(
                    t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<WarehouseTask?> GetTaskByReferenceAsync(
                WarehouseTaskReferenceType referenceType,
                int referenceId,
                int warehouseId,
                WarehouseTaskType taskType)
        {
            return await _context.WarehouseTasks
                .FirstOrDefaultAsync(t =>
                    t.ReferenceType == referenceType &&
                    t.ReferenceId == referenceId &&
                    t.WarehouseId == warehouseId &&
                    t.Type == taskType);
                    }
                }
}