using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ICategoryService
    {
        Task CreateCategoryAsync(
            CreateCategoryDto request);

        Task UpdateCategoryAsync(
            int id,
            UpdateCategoryDto request);

        Task DeleteCategoryAsync(
            int id);

        Task<CategoryResponseDto>
            GetCategoryByIdAsync(
                int id);

        Task<IEnumerable<CategoryResponseDto>>
            GetCategoriesAsync();
    }
}