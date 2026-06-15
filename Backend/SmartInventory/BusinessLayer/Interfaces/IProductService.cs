using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IProductService
    {
        Task CreateProductAsync(
            CreateProductDto request);

        Task UpdateProductAsync(
            int id,
            UpdateProductDto request);

        Task DeleteProductAsync(
            int id);

        Task<ProductResponseDto>
            GetProductByIdAsync(
                int id);

        Task<PagedResponseDto<ProductResponseDto>>
            GetProductsAsync(
                PaginationParams pagination,ProductFilterDto filter);
    }
}