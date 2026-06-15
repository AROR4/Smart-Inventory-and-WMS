using AutoMapper;
using Moq;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartInventory.Tests;

[TestFixture]
public class InventoryServiceTests
{
    private ApplicationDbContext _context = null!;

    private IInventoryRepository _inventoryRepository = null!;

    private IProductRepository _productRepository = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private IStockMovementRepository _stockMovementRepository = null!;

    private IRepository<Category> _categoryRepository = null!;

    private IRepository<Company> _companyRepository = null!;

    private Mock<ICurrentUserService> _mockCurrentUserService = null!;

    private ILowStockAlertService _lowStockAlertService = null!;

    private IMapper _mapper = null!;

    private IInventoryService _inventoryService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);

        _inventoryRepository = new InventoryRepository(_context);
        _productRepository = new ProductRepository(_context);
        _warehouseRepository = new Repository<Warehouse>(_context);
        _stockMovementRepository = new StockMovementRepository(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(m => m.Role).Returns("Admin");

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();

        var lowStockAlertRepository = new LowStockAlertRepository(_context);
        _lowStockAlertService = new LowStockAlertService(
            lowStockAlertRepository,
            _inventoryRepository,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<LowStockAlertService>>().Object,
            _mapper);

        _inventoryService = new InventoryService(
            _inventoryRepository,
            _productRepository,
            _warehouseRepository,
            _stockMovementRepository,
            _mockCurrentUserService.Object,
            _lowStockAlertService,
            _context,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<InventoryService>>().Object,
            _mapper);
    }

    #region AddStock Tests

    [Test]
    public async Task AddStock_Success()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);

        var request = new AddStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 50
        };

        await _inventoryService.AddStockAsync(request);

        var inventory = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory!.Quantity, Is.EqualTo(50));
    }

    [Test]
    public void AddStock_ProductNotFound()
    {
        var request = new AddStockDto
        {
            ProductId = 999,
            WarehouseId = 1,
            Quantity = 50
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _inventoryService.AddStockAsync(request));
    }

    [Test]
    public async Task AddStock_WarehouseNotFound()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);

        var request = new AddStockDto
        {
            ProductId = 1,
            WarehouseId = 999,
            Quantity = 50
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _inventoryService.AddStockAsync(request));
    }

    [Test]
    public async Task AddStock_IncompatibleStorageType()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.ColdStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Dry Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);

        var request = new AddStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 50
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _inventoryService.AddStockAsync(request));
    }

    [Test]
    public async Task AddStock_InsufficientCapacity()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 1000,
            Width = 1000,
            Height = 1000,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Small Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100,
            AvailableCapacity = 100,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);

        var request = new AddStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 50
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _inventoryService.AddStockAsync(request));
    }

    [Test]
    public async Task AddStock_ExistingInventory_UpdatesQuantity()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            IsActive = true
        };

        var company = new Company
        {
            Id = 1,
            Name = "TechCorp",
            IsActive = true
        };

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);

        await _inventoryRepository.AddAsync(
            new Inventory
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 20,
                LastUpdated = DateTime.Now
            });

        var request = new AddStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 30
        };

        await _inventoryService.AddStockAsync(request);

        var inventory =
            await _inventoryRepository
                .GetInventoryAsync(1, 1);

        Assert.That(
            inventory,
            Is.Not.Null);

        Assert.That(
            inventory!.Quantity,
            Is.EqualTo(50));
    }

    #endregion

    #region GetInventory Tests

    [Test]
    public async Task GetInventory_Success()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var result = await _inventoryService.GetInventoryAsync(
            new PaginationParams { PageNumber = 1, PageSize = 10 },
            new InventoryFilterDto());

        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.TotalRecords, Is.EqualTo(1));
    }

    [Test]
    public async Task GetInventory_WithFilter()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var result = await _inventoryService.GetInventoryAsync(
            new PaginationParams { PageNumber = 1, PageSize = 10 },
            new InventoryFilterDto { WarehouseId = 1 });

        Assert.That(result.Data, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetInventory_Empty()
    {
        var result = await _inventoryService.GetInventoryAsync(
            new PaginationParams { PageNumber = 1, PageSize = 10 },
            new InventoryFilterDto());

        Assert.That(result.Data, Is.Empty);
        Assert.That(result.TotalRecords, Is.EqualTo(0));
    }

    [Test]
    public async Task GetInventory_FilterByProductId()
    {
        
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);


        var result = await _inventoryService.GetInventoryAsync(
            new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            },
            new InventoryFilterDto
            {
                ProductId = 1
            });

        Assert.That(result.TotalRecords, Is.EqualTo(1));
        Assert.That(result.Data.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetInventory_SearchByProductName()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            IsActive = true
        };

        var company = new Company
        {
            Id = 1,
            Name = "TechCorp",
            IsActive = true
        };

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var result = await _inventoryService.GetInventoryAsync(
            new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            },
            new InventoryFilterDto
            {
                Search = "Laptop"
            });

        Assert.That(result.TotalRecords, Is.EqualTo(1));
    }

    [Test]
    public async Task GetInventory_WarehouseManager_RestrictsToAssignedWarehouse()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse1 = new Warehouse
        {
            Id = 1,
            Name = "Warehouse 1",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var warehouse2 = new Warehouse
        {
            Id = 2,
            Name = "Warehouse 2",
            AddressLine1 = "456 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory1 = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse1
        };

        var inventory2 = new Inventory
        {
            Id = 2,
            ProductId = 1,
            WarehouseId = 2,
            Quantity = 200,
            Product = product,
            Warehouse = warehouse2
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse1);
        await _warehouseRepository.AddAsync(warehouse2);
        await _inventoryRepository.AddAsync(inventory1);
        await _inventoryRepository.AddAsync(inventory2);

        _mockCurrentUserService.Setup(m => m.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(m => m.AssignedWarehouseId).Returns(1);

        // Under new authorization rules, requesting other warehouse's inventory throws ForbiddenException
        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _inventoryService.GetInventoryAsync(
                new PaginationParams { PageNumber = 1, PageSize = 10 },
                new InventoryFilterDto { WarehouseId = 2 }));
    }



    #endregion

    #region RemoveStock Tests

    [Test]
    public async Task RemoveStock_Success()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var request = new RemoveStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 30
        };

        await _inventoryService.RemoveStockAsync(request);

        var updated = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(updated!.Quantity, Is.EqualTo(70));
    }

    [Test]
    public async Task RemoveStock_InsufficientStock()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 10,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var request = new RemoveStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 50
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _inventoryService.RemoveStockAsync(request));
    }

    [Test]
    public void RemoveStock_InventoryNotFound()
    {
        var request = new RemoveStockDto
        {
            ProductId = 999,
            WarehouseId = 999,
            Quantity = 10
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _inventoryService.RemoveStockAsync(request));
    }

    #endregion

    #region AdjustStock Tests

    [Test]
    public async Task AdjustStock_IncreaseQuantity()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var request = new AdjustStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            NewQuantity = 150
        };

        await _inventoryService.AdjustStockAsync(request);

        var updated = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(updated!.Quantity, Is.EqualTo(150));
    }

    [Test]
    public async Task AdjustStock_DecreaseQuantity()
    {
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 10,
            Width = 20,
            Height = 2,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 100000,
            AvailableCapacity = 100000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 100,
            Product = product,
            Warehouse = warehouse
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var request = new AdjustStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            NewQuantity = 50
        };

        await _inventoryService.AdjustStockAsync(request);

        var updated = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(updated!.Quantity, Is.EqualTo(50));
    }

    [Test]
    public void AdjustStock_InventoryNotFound()
    {
        var request = new AdjustStockDto
        {
            ProductId = 999,
            WarehouseId = 999,
            NewQuantity = 100
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _inventoryService.AdjustStockAsync(request));
    }


    [Test]
    public async Task AdjustStock_InsufficientCapacity()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            IsActive = true
        };

        var company = new Company
        {
            Id = 1,
            Name = "TechCorp",
            IsActive = true
        };

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP001",
            Barcode = "BAR001",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 100,
            Width = 100,
            Height = 100,
            IsActive = true
        };

        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Warehouse",
            Capacity = 100,
            AvailableCapacity = 100,
            ReservedCapacity = 0,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 1
        };

        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _warehouseRepository.AddAsync(warehouse);
        await _inventoryRepository.AddAsync(inventory);

        var request = new AdjustStockDto
        {
            ProductId = 1,
            WarehouseId = 1,
            NewQuantity = 100
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _inventoryService.AdjustStockAsync(request));
    }

    #endregion

    #region Authorization Tests

    [Test]
    public void GetInventory_WarehouseManager_OtherWarehouse_ThrowsForbidden()
    {
        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(1);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new InventoryFilterDto { WarehouseId = 2 };

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _inventoryService.GetInventoryAsync(pagination, filter));
    }

    [Test]
    public async Task GetInventory_WarehouseManager_OwnWarehouse_Success()
    {
        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(1);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new InventoryFilterDto { WarehouseId = 1 };

        var result = await _inventoryService.GetInventoryAsync(pagination, filter);
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
