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
public class StockMovementServiceTests
{

    private ApplicationDbContext _context = null!;

    private IStockMovementRepository
        _stockMovementRepository = null!;

    private IRepository<Product>
        _productRepository = null!;

    private IRepository<Category>
        _categoryRepository = null!;

    private IRepository<Company>
        _companyRepository = null!;

    private IRepository<Warehouse>
        _warehouseRepository = null!;

    private IRepository<User>
        _userRepository = null!;

    private IMapper _mapper = null!;

    private StockMovementService
        _stockMovementService = null!;

    [SetUp]
    public void Setup()
    {
        var options =
            new DbContextOptionsBuilder<
                ApplicationDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;

        _context =
            new ApplicationDbContext(
                options);

        _stockMovementRepository =
            new StockMovementRepository(
                _context);

        _productRepository =
            new Repository<Product>(
                _context);

        _categoryRepository =
            new Repository<Category>(
                _context);

        _companyRepository =
            new Repository<Company>(
                _context);

        _warehouseRepository =
            new Repository<Warehouse>(
                _context);

        _userRepository =
            new Repository<User>(
                _context);

        var mapperConfig =
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<
                    MappingProfile>();
            });

        _mapper =
            mapperConfig.CreateMapper();

        _stockMovementService =
            new StockMovementService(
                _stockMovementRepository,
                _mapper);
    }

    #region GetStockMovements Tests

    [Test]
    public async Task GetStockMovements_AllFiltersAndSearch()
    {
        var user =
            await CreateUser();

        var warehouse =
            await CreateWarehouse();

        var product =
            await CreateProduct();

        await _stockMovementRepository
            .AddAsync(
                new StockMovement
                {
                    ProductId =
                        product.Id,

                    WarehouseId =
                        warehouse.Id,

                    CreatedByUserId =
                        user.Id,

                    Quantity = 10,

                    Type =
                        StockMovementType.StockIn,

                    Reason =
                        "Purchase"
                });

        await _stockMovementRepository
            .AddAsync(
                new StockMovement
                {
                    ProductId =
                        product.Id,

                    WarehouseId =
                        warehouse.Id,

                    CreatedByUserId =
                        user.Id,

                    Quantity = 5,

                    Type =
                        StockMovementType.StockOut,

                    Reason =
                        "Sale"
                });

        var result =
            await _stockMovementService
                .GetStockMovementsAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 10
                    },
                    new StockMovementFilterDto
                    {
                        ProductId =
                            product.Id,

                        WarehouseId =
                            warehouse.Id,

                        Type =
                            StockMovementType.StockIn,

                        Search =
                            "Laptop"
                    });

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(1));

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(1));

        Assert.That(
            result.Data.First().Type,
            Is.EqualTo(
                StockMovementType.StockIn
                    .ToString()));
    }


    [Test]
    public async Task GetStockMovements_Pagination()
    {
        var user =
            await CreateUser();

        var warehouse =
            await CreateWarehouse();

        var product =
            await CreateProduct();

        for (int i = 1; i <= 5; i++)
        {
            await _stockMovementRepository
                .AddAsync(
                    new StockMovement
                    {
                        ProductId =
                            product.Id,

                        WarehouseId =
                            warehouse.Id,

                        CreatedByUserId =
                            user.Id,

                        Quantity =
                            i,

                        Type =
                            StockMovementType.StockIn,

                        Reason =
                            $"Movement {i}"
                    });
        }

        var result =
            await _stockMovementService
                .GetStockMovementsAsync(
                    new PaginationParams
                    {
                        PageNumber = 2,
                        PageSize = 2
                    },
                    new StockMovementFilterDto());

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(5));

        Assert.That(
            result.PageNumber,
            Is.EqualTo(2));

        Assert.That(
            result.PageSize,
            Is.EqualTo(2));

        Assert.That(
            result.TotalPages,
            Is.EqualTo(3));

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(2));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<User>
        CreateUser()
    {
        var user =
            new User
            {
                Name = "Raghav",
                Email = "raghav@test.com"
            };

        await _userRepository
            .AddAsync(user);

        return user;
    }

    private async Task<Warehouse>
        CreateWarehouse()
    {
        var warehouse =
            new Warehouse
            {
                Name = "Main Warehouse",
                IsActive = true
            };

        await _warehouseRepository
            .AddAsync(warehouse);

        return warehouse;
    }

    private async Task<Product>
        CreateProduct()
    {
        var category =
            new Category
            {
                Name = "Electronics",
                IsActive = true
            };

        await _categoryRepository
            .AddAsync(category);

        var company =
            new Company
            {
                Name = "Dell",
                IsActive = true
            };

        await _companyRepository
            .AddAsync(company);

        var product =
            new Product
            {
                Name = "Laptop",
                SKU = Guid.NewGuid().ToString(),
                Barcode = Guid.NewGuid().ToString(),
                CategoryId = category.Id,
                CompanyId = company.Id,
                IsActive = true
            };

        await _productRepository
            .AddAsync(product);

        return product;
    }

   
}
