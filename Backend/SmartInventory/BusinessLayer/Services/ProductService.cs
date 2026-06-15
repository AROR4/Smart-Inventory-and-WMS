using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Company> _companyRepository;
        private readonly ILogger<ProductService> _logger;
        private readonly IMapper _mapper;
        public ProductService(IProductRepository productRepository, IRepository<Category> categoryRepository, IRepository<Company> companyRepository, ILogger<ProductService> logger, IMapper mapper)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _companyRepository = companyRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task CreateProductAsync(
            CreateProductDto request)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(request.CategoryId);

            if (category == null || !category.IsActive)
            {
                _logger.LogWarning("Failed to create product. Category with ID {CategoryId} not found or inactive.", request.CategoryId);
                throw new NotFoundException(
                    "Category not found.");
            }

            var company =await _companyRepository.GetByIdAsync(request.CompanyId);

            if (company == null || !company.IsActive)
            {
                _logger.LogWarning("Failed to create product. Company with ID {CompanyId} not found or inactive.", request.CompanyId);
                throw new NotFoundException(
                    "Company not found.");
            }

            if (await _productRepository.ExistsAsync(
                    p => p.SKU == request.SKU))
            {
                _logger.LogWarning("Failed to create product. SKU {SKU} already exists.", request.SKU);
                throw new ConflictException(
                    "SKU already exists.");
            }

            if (await _productRepository.ExistsAsync(
                    p => p.Barcode == request.Barcode))
            {
                _logger.LogWarning("Failed to create product. Barcode {Barcode} already exists.", request.Barcode);
                throw new ConflictException(
                    "Barcode already exists.");
            }

            if(!string.IsNullOrWhiteSpace(request.ModelNumber)){

            if (await _productRepository.ExistsAsync(
                    p => p.ModelNumber == request.ModelNumber))
            {
                _logger.LogWarning("Failed to create product. Model number {ModelNumber} already exists.", request.ModelNumber);
                throw new ConflictException(
                    "Model number already exists.");
            }
            }

            var product =_mapper.Map<Product>(request);

            await _productRepository.AddAsync(product);
            _logger.LogInformation("Product {ProductName} successfully created with ID {ProductId}.", product.Name, product.Id);
        }

        public async Task DeleteProductAsync(
            int id)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(id);

            if (product == null || !product.IsActive)
            {
                _logger.LogWarning("Failed to delete product. Product with ID {ProductId} not found or inactive.", id);
                throw new NotFoundException(
                    "Product not found.");
            }

            product.IsActive = false;

            await _productRepository
                .UpdateAsync(product);
            _logger.LogInformation("Product with ID {ProductId} successfully marked as inactive.", id);
        }

        public async Task<ProductResponseDto> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetProductWithDetailsAsync(id);
            if (product == null || !product.IsActive)
            {
                _logger.LogWarning("Product with ID {ProductId} not found or inactive.", id);
                throw new NotFoundException("Product not found.");
            }
            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<PagedResponseDto<ProductResponseDto>>
            GetProductsAsync(
                PaginationParams pagination,
                ProductFilterDto filter)
        {
            _logger.LogInformation("Fetching products page {PageNumber} with page size {PageSize}.", pagination.PageNumber, pagination.PageSize);
            var products =
                await _productRepository
                    .GetProductsWithDetailsAsync();

            products = products
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                products = products.Where(p =>
                    p.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    p.SKU.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    p.Barcode.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (!string.IsNullOrWhiteSpace(p.ModelNumber)
                        && p.ModelNumber.Contains(
                            filter.Search,
                            StringComparison.OrdinalIgnoreCase))
                    ||
                    p.Company.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (filter.CategoryId.HasValue)
            {
                products = products.Where(
                    p => p.CategoryId ==
                        filter.CategoryId.Value);
            }

            if (filter.StorageType.HasValue)
            {
                products = products.Where(
                    p => p.RequiredStorageType ==
                        filter.StorageType.Value);
            }

            if (filter.CompanyId.HasValue)
            {
                products = products.Where(
                    p => p.CompanyId ==
                        filter.CompanyId.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                products = products.Where(
                    p => p.UnitPrice >=
                        filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                products = products.Where(
                    p => p.UnitPrice <=
                        filter.MaxPrice.Value);
            }

            var totalRecords = products.Count();

            var pagedProducts = products
                .Skip(
                    (pagination.PageNumber - 1)
                    * pagination.PageSize)
                .Take(
                    pagination.PageSize);

            return new PagedResponseDto<ProductResponseDto>
            {
                Data = _mapper.Map<
                    IEnumerable<ProductResponseDto>>(
                    pagedProducts),

                PageNumber =
                    pagination.PageNumber,

                PageSize =
                    pagination.PageSize,

                TotalRecords =
                    totalRecords,

                TotalPages =
                    (int)Math.Ceiling(
                        totalRecords /
                        (double)pagination.PageSize)
            };
        }

        public async Task UpdateProductAsync(
            int id,
            UpdateProductDto request)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(id);

            if (product == null || !product.IsActive)
            {
                _logger.LogWarning("Failed to update product. Product with ID {ProductId} not found or inactive.", id);
                throw new NotFoundException(
                    "Product not found.");
            }

            var category =
                await _categoryRepository
                    .GetByIdAsync(request.CategoryId);

            if (category == null || !category.IsActive)
            {
                _logger.LogWarning("Failed to update product with ID {ProductId}. Category with ID {CategoryId} not found or inactive.", id, request.CategoryId);
                throw new NotFoundException(
                    "Category not found.");
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.UnitPrice = request.UnitPrice;
            product.CategoryId = request.CategoryId;
            product.ReorderLevel =request.ReorderLevel;
            product.Length= request.Length;
            product.Width= request.Width;
            product.Height= request.Height;
            product.UnitsPerCarton= request.UnitsPerCarton;
            product.RequiredStorageType = request.RequiredStorageType;

            await _productRepository
                .UpdateAsync(product);
            _logger.LogInformation("Product with ID {ProductId} successfully updated.", id);
        }
    }
}