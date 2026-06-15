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
public class LowStockAlertServiceTests
{

   private ILowStockAlertRepository _alertRepository = null!;
    private IInventoryRepository _inventoryRepository = null!;

    private IProductRepository _productRepository = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private LowStockAlertService _service = null!;

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


        _alertRepository = new LowStockAlertRepository(_context);
        _inventoryRepository = new InventoryRepository(_context);
        _productRepository = new ProductRepository(_context);
        _warehouseRepository = new Repository<Warehouse>(_context);

        _service = new LowStockAlertService(
            _alertRepository,
            _inventoryRepository,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<LowStockAlertService>>().Object,
            _mapper);
    }

    
    #region CheckAndUpdateAlert Tests

    [Test]
    public async Task CheckAndUpdateAlert_InventoryNotFound()
    {
        await _service
            .CheckAndUpdateAlertAsync(
                1,
                1);

        var alerts =
            await _alertRepository
                .GetActiveAlertsAsync();

        Assert.That(
            alerts.Count(),
            Is.EqualTo(0));
    }

    [Test]

    public async Task CheckAndUpdateAlert_AlertCreated()
    {
        
    
        var product = new Product
    {
        Name = "Laptop",
        ReorderLevel = 10,
        IsActive = true
    };

    await _productRepository.AddAsync(product);

    var warehouse = new Warehouse
    {
        Name = "WH1",
        IsActive = true
    };

    await _warehouseRepository.AddAsync(warehouse);

    await _inventoryRepository.AddAsync(
        new Inventory
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 5
        });

        await _service.CheckAndUpdateAlertAsync(
        product.Id,
        warehouse.Id);

        var alerts =
            await _alertRepository
                .GetActiveAlertsAsync();

        Assert.That(
            alerts.Count(),
            Is.EqualTo(1));
    }

    [Test]

    public async Task CheckAndUpdateAlert_UpdateExistingAlert()
    {

            var product = new Product
            {
                Name = "Laptop",
                ReorderLevel = 10,
                IsActive = true
            };

            await _productRepository.AddAsync(product);

            var warehouse = new Warehouse
            {
                Name = "WH1",
                IsActive = true
            };

            await _warehouseRepository.AddAsync(warehouse);

            var inventory = new Inventory
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 5
                };
            await _inventoryRepository.AddAsync(inventory);

        await _alertRepository.AddAsync(
        new LowStockAlert
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            CurrentQuantity = 8,
            ReorderLevel = 10,
            IsResolved = false
        });

        inventory.Quantity = 4;

        await _inventoryRepository.UpdateAsync(
            inventory);

            await _service.CheckAndUpdateAlertAsync(
            product.Id,
            warehouse.Id);

        var alert =
    await _alertRepository
        .GetActiveAlertAsync(
            product.Id,
            warehouse.Id);

        Assert.That(
            alert!.CurrentQuantity,
            Is.EqualTo(4));
    }


    [Test]
    public async Task CheckAndUpdateAlert_ResolveAlert()
    {
        var product = new Product
        {
            Name = "Laptop",
            ReorderLevel = 10,
            IsActive = true
        };

        await _productRepository
            .AddAsync(product);

        var warehouse = new Warehouse
        {
            Name = "Warehouse 1",
            IsActive = true
        };

        await _warehouseRepository
            .AddAsync(warehouse);

        var inventory = new Inventory
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 50
        };

        await _inventoryRepository
            .AddAsync(inventory);

        await _alertRepository
            .AddAsync(
                new LowStockAlert
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    CurrentQuantity = 5,
                    ReorderLevel = 10,
                    IsResolved = false
                });

        await _service
            .CheckAndUpdateAlertAsync(
                product.Id,
                warehouse.Id);

        var alert = (await _alertRepository
            .FindAsync(
                a => a.ProductId == product.Id &&
                    a.WarehouseId == warehouse.Id))
            .FirstOrDefault();

        Assert.That(
            alert,
            Is.Not.Null);

        Assert.That(
            alert!.IsResolved,
            Is.True);
    }


    [Test]
    public async Task CheckAndUpdateAlert_HealthyInventory_NoAlert()
    {
        var product = new Product
        {
            Name = "Laptop",
            ReorderLevel = 10,
            IsActive = true
        };

        await _productRepository
            .AddAsync(product);

        var warehouse = new Warehouse
        {
            Name = "Warehouse 1",
            IsActive = true
        };

        await _warehouseRepository
            .AddAsync(warehouse);

        await _inventoryRepository
            .AddAsync(
                new Inventory
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 100
                });

        await _service
            .CheckAndUpdateAlertAsync(
                product.Id,
                warehouse.Id);

        var alerts =
            await _alertRepository
                .GetActiveAlertsAsync();

        Assert.That(
            alerts.Count(),
            Is.EqualTo(0));
    }

    [Test]
    public async Task GetActiveAlertsAsync_ReturnsPagedAlerts()
    {
        var product = new Product
        {
            Name = "Laptop",
            SKU = "SKU1",
            Barcode = "BAR1",
            CategoryId = 1,
            CompanyId = 1,
            ReorderLevel = 10,
            IsActive = true
        };

        await _productRepository.AddAsync(product);

        var warehouse = new Warehouse
        {
            Name = "Warehouse 1",
            Capacity = 1000,
            AvailableCapacity = 1000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _warehouseRepository.AddAsync(warehouse);

        for (int i = 1; i <= 3; i++)
        {
            await _alertRepository.AddAsync(
                new LowStockAlert
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    CurrentQuantity = i,
                    ReorderLevel = 10,
                    IsResolved = false
                });
        }

        var result =
            await _service.GetActiveAlertsAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 2
                });

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(3));

        Assert.That(
            result.PageNumber,
            Is.EqualTo(1));

        Assert.That(
            result.PageSize,
            Is.EqualTo(2));

        Assert.That(
            result.TotalPages,
            Is.EqualTo(2));

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

   
}
