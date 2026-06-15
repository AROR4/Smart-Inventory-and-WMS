using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CategoryController
        : ControllerBase
    {
        private readonly ICategoryService
            _categoryService;

        public CategoryController(
            ICategoryService categoryService)
        {
            _categoryService =
                categoryService;
        }

        [HttpPost]
        public async Task<IActionResult>
            CreateCategory(
                CreateCategoryDto request)
        {
            await _categoryService
                .CreateCategoryAsync(request);

            return Ok(new
            {
                Message =
                    "Category created successfully."
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult>
            UpdateCategory(
                int id,
                UpdateCategoryDto request)
        {
            await _categoryService
                .UpdateCategoryAsync(
                    id,
                    request);

            return Ok(new
            {
                Message =
                    "Category updated successfully."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>
            DeleteCategory(
                int id)
        {
            await _categoryService
                .DeleteCategoryAsync(id);

            return Ok(new
            {
                Message =
                    "Category deleted successfully."
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetCategory(
                int id)
        {
            return Ok(
                await _categoryService
                    .GetCategoryByIdAsync(id));
        }

        [HttpGet]
        public async Task<IActionResult>
            GetCategories()
        {
            return Ok(
                await _categoryService
                    .GetCategoriesAsync());
        }
    }
}