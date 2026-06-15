using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IPurchaseOrderRepository
    : IRepository<PurchaseOrder>
    {
        Task<PurchaseOrder?>
            GetPurchaseOrderWithDetailsAsync(
                int id);

        Task<IEnumerable<PurchaseOrder>>
            GetPurchaseOrdersWithDetailsAsync();

        Task<PurchaseOrder?>
            GetByOrderNumberAsync(
                string orderNumber);

        Task<IEnumerable<PurchaseOrder>>
            GetSupplierPendingOrdersAsync(
                int supplierId);

        Task<IEnumerable<PurchaseOrder>>
            GetSupplierOrderHistoryAsync(
                int supplierId);
            }
}