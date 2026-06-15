using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventory.Tests;

[TestFixture]
public class ProductServiceTests
{
    private IProductRepository _productRepository = null!;

    private IRepository<Category> _categoryRepository = null!;

    private IRepository<Company> _companyRepository = null!;
    private ProductService _productService = null!;
    private ApplicationDbContext _context = null!;

    private IMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        _context =new ApplicationDbContext(options);
        
        var mapperConfig =
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

        _mapper =
            mapperConfig.CreateMapper();
       
        _productRepository = new ProductRepository(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);

        _productService = new ProductService(
            _productRepository,
            _categoryRepository,
            _companyRepository,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<ProductService>>().Object,
            _mapper);
    }


    #region CreateProduct Tests

    [Test]
    public void CreateProduct_CategoryNotFound()
    {
        var request = new CreateProductDto
        {
            Name = "Laptop",
            CategoryId = 999,
            CompanyId = 1,
            SKU = "SKU1",
            Barcode = "BAR1"
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService
                    .CreateProductAsync(request));
    }


    [Test]
    public async Task CreateProduct_CategoryInactive()
    {
        var category = new Category
        {
            Name = "Electronics",
            IsActive = false
        };

        await _categoryRepository.AddAsync(category);

        var company = await CreateCompany();

        var request = new CreateProductDto
        {
            Name = "Laptop",
            CategoryId = category.Id,
            CompanyId = company.Id,
            SKU = "SKU1",
            Barcode = "BAR1"
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService
                    .CreateProductAsync(request));
    }

    [Test]
    public async Task CreateProduct_CompanyNotFound()
    {
        var category = await CreateCategory();

        var request = new CreateProductDto
        {
            Name = "Laptop",
            CategoryId = category.Id,
            CompanyId = 999,
            SKU = "SKU1",
            Barcode = "BAR1"
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService
                    .CreateProductAsync(request));
    }

    [Test]
    public async Task CreateProduct_CompanyInactive()
    {
        var category = await CreateCategory();

        var company = new Company
        {
            Name = "Dell",
            IsActive = false
        };

        await _companyRepository.AddAsync(company);

        var request = new CreateProductDto
        {
            Name = "Laptop",
            CategoryId = category.Id,
            CompanyId = company.Id,
            SKU = "SKU1",
            Barcode = "BAR1"
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService
                    .CreateProductAsync(request));
    }

    [Test]

    public async Task CreateProduct_DuplicateSku()
    {
        var category = await CreateCategory();

        var company = await CreateCompany();

        await _productRepository.AddAsync(

            new Product

            {

                Name = "Old",

                SKU = "SKU1",

                Barcode = "BAR1",

                CategoryId = category.Id,

                CompanyId = company.Id,

                IsActive = true

            });



        var request = new CreateProductDto

        {

            Name = "New",

            SKU = "SKU1",

            Barcode = "BAR2",

            CategoryId = category.Id,

            CompanyId = company.Id

        };



        Assert.ThrowsAsync<ConflictException>(

            async () =>

                await _productService

                    .CreateProductAsync(request));

    }



    [Test]

    public async Task CreateProduct_DuplicateBarcode()

    {

        var category = await CreateCategory();

        var company = await CreateCompany();



        await _productRepository.AddAsync(

            new Product

            {

                Name = "Old",

                SKU = "SKU1",

                Barcode = "BAR1",

                CategoryId = category.Id,

                CompanyId = company.Id,

                IsActive = true

            });



        var request = new CreateProductDto

        {

            Name = "New",

            SKU = "SKU2",

            Barcode = "BAR1",

            CategoryId = category.Id,

            CompanyId = company.Id

        };



        Assert.ThrowsAsync<ConflictException>(

            async () =>

                await _productService

                    .CreateProductAsync(request));

    }



    [Test]

    public async Task CreateProduct_DuplicateModelNumber()

    {

        var category = await CreateCategory();

        var company = await CreateCompany();



        await _productRepository.AddAsync(

            new Product

            {

                Name = "Old",

                SKU = "SKU1",

                Barcode = "BAR1",

                ModelNumber = "M100",

                CategoryId = category.Id,

                CompanyId = company.Id,

                IsActive = true

            });



        var request = new CreateProductDto

        {

            Name = "New",

            SKU = "SKU2",

            Barcode = "BAR2",

            ModelNumber = "M100",

            CategoryId = category.Id,

            CompanyId = company.Id

        };



        Assert.ThrowsAsync<ConflictException>(

            async () =>

                await _productService

                    .CreateProductAsync(request));

    }



    [Test]

    public async Task CreateProduct_Success()

    {

        var category = await CreateCategory();

        var company = await CreateCompany();



        var request = new CreateProductDto

        {

            Name = "Laptop",

            SKU = "SKU1",

            Barcode = "BAR1",

            ModelNumber = "M100",

            CategoryId = category.Id,

            CompanyId = company.Id

        };



        await _productService

            .CreateProductAsync(request);



        var products =

            await _productRepository

                .GetAllAsync();



        Assert.That(

            products.Count(),

            Is.EqualTo(1));

    }



    [Test]

    public async Task CreateProduct_SuccessWithoutModelNumber()

    {

        var category = await CreateCategory();

        var company = await CreateCompany();



        var request = new CreateProductDto

        {

            Name = "Laptop",

            SKU = "SKU1",

            Barcode = "BAR1",

            CategoryId = category.Id,

            CompanyId = company.Id

        };



        await _productService

            .CreateProductAsync(request);



        var products =

            await _productRepository

                .GetAllAsync();



        Assert.That(

            products.Count(),

            Is.EqualTo(1));

    }



    #endregion



    #region DeleteProduct Tests

    [Test]

    public void DeleteProduct_NotFound()

    {

        Assert.ThrowsAsync<NotFoundException>(

            async () =>

                await _productService

                    .DeleteProductAsync(999));

    }



    [Test]

    public async Task DeleteProduct_Inactive()

    {

        var product = new Product

        {

            Name = "Laptop",

            IsActive = false

        };



        await _productRepository.AddAsync(product);



        Assert.ThrowsAsync<NotFoundException>(

            async () =>

                await _productService

                    .DeleteProductAsync(

                        product.Id));

    }



    [Test]

    public async Task DeleteProduct_Success()

    {

        var product = new Product

        {

            Name = "Laptop",

            IsActive = true

        };



        await _productRepository.AddAsync(product);



        await _productService

            .DeleteProductAsync(

                product.Id);



        var updated =

            await _productRepository

                .GetByIdAsync(

                    product.Id);



        Assert.That(

            updated!.IsActive,

            Is.False);

    }

    #endregion



    #region GetProductById Tests

    [Test]

    public void GetProductById_NotFound()

    {

        Assert.ThrowsAsync<NotFoundException>(

            async () =>

                await _productService

                    .GetProductByIdAsync(999));

    }



    [Test]

    public async Task GetProductById_Inactive()

    {

        var product = new Product

        {

            Name = "Laptop",

            IsActive = false

        };



        await _productRepository.AddAsync(product);



        Assert.ThrowsAsync<NotFoundException>(

            async () =>

                await _productService

                    .GetProductByIdAsync(

                        product.Id));

    }



    [Test]

    public async Task GetProductById_Success()

    {

        var category = await CreateCategory();

        var company = await CreateCompany();



        var product = new Product

        {

            Name = "Laptop",

            SKU = "SKU1",

            Barcode = "BAR1",

            CategoryId = category.Id,

            CompanyId = company.Id,

            IsActive = true

        };



        await _productRepository.AddAsync(product);



        var result =

            await _productService

                .GetProductByIdAsync(

                    product.Id);



        Assert.That(

            result.Name,

            Is.EqualTo("Laptop"));

    }


    #endregion

    #region GetProducts Tests

    [Test]
    public async Task GetProducts_AllFiltersApplied()
    {
        var category = await CreateCategory();
        var company = await CreateCompany();

        await _productRepository.AddAsync(
            new Product
            {
                Name = "Gaming Laptop",
                SKU = "SKU1",
                Barcode = "BAR1",
                CategoryId = category.Id,
                CompanyId = company.Id,
                UnitPrice = 500,
                RequiredStorageType = StorageType.DryStorage,
                IsActive = true
            });

        await _productRepository.AddAsync(
            new Product
            {
                Name = "Monitor",
                SKU = "SKU2",
                Barcode = "BAR2",
                CategoryId = category.Id,
                CompanyId = company.Id,
                UnitPrice = 5000,
                RequiredStorageType = StorageType.DryStorage,
                IsActive = true
            });

        var result =
            await _productService.GetProductsAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 10
                },
                new ProductFilterDto
                {
                    CategoryId = category.Id,
                    CompanyId = company.Id,
                    MinPrice = 100,
                    MaxPrice = 1000,
                    StorageType = StorageType.DryStorage
                });
        

        Assert.That(result.TotalRecords,
            Is.EqualTo(1));

        Assert.That(result.Data.Count(),
            Is.EqualTo(1));
    }


    [Test]
    public async Task GetProducts_SearchAndPagination()
    {
        var category = await CreateCategory();
        var company = await CreateCompany();

        for (int i = 1; i <= 5; i++)
        {
            await _productRepository.AddAsync(
                new Product
                {
                    Name = $"Laptop{i}",
                    SKU = $"SKU{i}",
                    Barcode = $"BAR{i}",
                    CategoryId = category.Id,
                    CompanyId = company.Id,
                    UnitPrice = 100,
                    IsActive = true
                });
        }

        await _productRepository.AddAsync(
            new Product
            {
                Name = "Monitor",
                SKU = "MON1",
                Barcode = "MONBAR",
                CategoryId = category.Id,
                CompanyId = company.Id,
                UnitPrice = 100,
                IsActive = true
            });

        var result =
            await _productService.GetProductsAsync(
                new PaginationParams
                {
                    PageNumber = 2,
                    PageSize = 2
                },
                new ProductFilterDto
                {
                    Search = "Laptop"
                });

        Assert.That(result.TotalRecords,
            Is.EqualTo(5));

        Assert.That(result.PageNumber,
            Is.EqualTo(2));

        Assert.That(result.PageSize,
            Is.EqualTo(2));

        Assert.That(result.TotalPages,
            Is.EqualTo(3));

        Assert.That(result.Data.Count(),
            Is.EqualTo(2));

        Assert.That(
            result.Data.All(
                p => p.Name.Contains("Laptop")),
            Is.True);
}

    [Test]
    public async Task GetProducts_Pagination()
    {
        var category = await CreateCategory();
        var company = await CreateCompany();

        for (int i = 1; i <= 5; i++)
        {
            await _productRepository.AddAsync(
                new Product
                {
                    Name = $"Product{i}",
                    SKU = $"SKU{i}",
                    Barcode = $"BAR{i}",
                    CategoryId = category.Id,
                    CompanyId = company.Id,
                    UnitPrice = 100,
                    IsActive = true
                });
        }

        var result =
            await _productService.GetProductsAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 3
                },
                new ProductFilterDto());

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(5));

        Assert.That(
            result.PageNumber,
            Is.EqualTo(1));

        Assert.That(
            result.PageSize,
            Is.EqualTo(3));

        Assert.That(
            result.TotalPages,
            Is.EqualTo(2));

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(3));
    }

    #endregion

    #region UpdateProduct Tests

    [Test]
    public async Task UpdateProduct_CategoryInactive()
    {
        var inactiveCategory = new Category
        {
            Name = "Electronics",
            IsActive = false
        };

        await _categoryRepository
            .AddAsync(inactiveCategory);

        var product = new Product
        {
            Name = "Laptop",
            IsActive = true
        };

        await _productRepository
            .AddAsync(product);

        var request = new UpdateProductDto
        {
            Name = "Updated",
            CategoryId = inactiveCategory.Id
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService
                    .UpdateProductAsync(
                        product.Id,
                        request));
    }

    [Test]
    public void UpdateProduct_ProductNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService.UpdateProductAsync(
                    999,
                    new UpdateProductDto()));
    }

    [Test]
    public async Task UpdateProduct_ProductInactive()
        {
            var product = new Product
            {
                Name = "Laptop",
                IsActive = false
            };

            await _productRepository.AddAsync(product);

            Assert.ThrowsAsync<NotFoundException>(
                async () =>
                    await _productService.UpdateProductAsync(
                        product.Id,
                        new UpdateProductDto()));
    }


    [Test]
    public async Task UpdateProduct_CategoryNotFound()
    {
        var product = new Product
        {
            Name = "Laptop",
            IsActive = true
        };

        await _productRepository.AddAsync(product);

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _productService.UpdateProductAsync(
                    product.Id,
                    new UpdateProductDto
                    {
                        CategoryId = 999
                    }));
    }

    [Test]
    public async Task UpdateProduct_Success()
    {
        var category = await CreateCategory();

        var product = new Product
        {
            Name = "Old Product",
            Description = "Old Desc",
            UnitPrice = 100,
            CategoryId = category.Id,
            ReorderLevel = 5,
            Length = 1,
            Width = 1,
            Height = 1,
            UnitsPerCarton = 1,
            RequiredStorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _productRepository.AddAsync(product);

        var request = new UpdateProductDto
        {
            Name = "New Product",
            Description = "New Desc",
            UnitPrice = 500,
            CategoryId = category.Id,
            ReorderLevel = 20,
            Length = 10,
            Width = 20,
            Height = 30,
            UnitsPerCarton = 50,
            RequiredStorageType = StorageType.ColdStorage
        };

        await _productService.UpdateProductAsync(
            product.Id,
            request);

        var updated =
            await _productRepository.GetByIdAsync(
                product.Id);

        Assert.That(updated!.Name,
            Is.EqualTo("New Product"));

        Assert.That(updated.Description,
            Is.EqualTo("New Desc"));

        Assert.That(updated.UnitPrice,
            Is.EqualTo(500));

        Assert.That(updated.ReorderLevel,
            Is.EqualTo(20));

        Assert.That(updated.Length,
            Is.EqualTo(10));

        Assert.That(updated.Width,
            Is.EqualTo(20));

        Assert.That(updated.Height,
            Is.EqualTo(30));

        Assert.That(updated.UnitsPerCarton,
            Is.EqualTo(50));

        Assert.That(updated.RequiredStorageType,
            Is.EqualTo(StorageType.ColdStorage));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
    private async Task<Category> CreateCategory()
    {
        var category = new Category
        {
            Name = "Electronics",
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        return category;
    }

    private async Task<Company> CreateCompany()
    {
        var company = new Company
        {
            Name = "Dell",
            IsActive = true
        };

        await _companyRepository.AddAsync(company);
        return company;
    }
   
}
