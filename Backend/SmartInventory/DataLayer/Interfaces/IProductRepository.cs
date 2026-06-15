using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.DataLayer.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetProductWithDetailsAsync(int id);

        Task<IEnumerable<Product>> GetProductsWithDetailsAsync();
    }
}