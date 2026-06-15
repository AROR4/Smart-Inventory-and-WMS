using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category>
            _categoryRepository;

        private readonly IMapper _mapper;

        public CategoryService(
            IRepository<Category> categoryRepository,
            IMapper mapper)
        {
            _categoryRepository =
                categoryRepository;

            _mapper = mapper;
        }

        public async Task CreateCategoryAsync(
            CreateCategoryDto request)
        {
            var exists =
                await _categoryRepository
                    .ExistsAsync(c =>c.IsActive &&
                        c.Name.ToLower() ==
                        request.Name.ToLower());

            if (exists)
            {
                throw new ConflictException(
                    "Category already exists.");
            }

            var category =
                _mapper.Map<Category>(request);

            await _categoryRepository
                .AddAsync(category);
        }

        public async Task UpdateCategoryAsync(
            int id,
            UpdateCategoryDto request)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException(
                    "Category not found.");
            }

            category.Name = request.Name;

            await _categoryRepository
                .UpdateAsync(category);
        }

        public async Task DeleteCategoryAsync(
            int id)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException(
                    "Category not found.");
            }

            category.IsActive = false;

            await _categoryRepository
                .UpdateAsync(category);
        }

        public async Task<CategoryResponseDto>
            GetCategoryByIdAsync(
                int id)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(id);

            if (category == null)
            {
                throw new NotFoundException(
                    "Category not found.");
            }

            return _mapper.Map<
                CategoryResponseDto>(
                category);
        }

        public async Task<
            IEnumerable<CategoryResponseDto>>
            GetCategoriesAsync()
        {
            var categories =
                await _categoryRepository
                    .FindAsync(c => c.IsActive);

            return _mapper.Map<
                IEnumerable<CategoryResponseDto>>(
                categories);
        }
    }
}