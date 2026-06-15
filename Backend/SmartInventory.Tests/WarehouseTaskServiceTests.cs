using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.Data;
using TaskStatusType = SmartInventoryManagement.Models.Enums.TaskStatusType;

namespace SmartInventory.Tests;

[TestFixture]
public class WarehouseTaskServiceTests
{
    private ApplicationDbContext _context = null!;
    private IWarehouseTaskRepository _warehouseTaskRepository = null!;
    private IRepository<Warehouse> _warehouseRepository = null!;
    private IProductRepository _productRepository = null!;
    private Mock<ICurrentUserService> _mockCurrentUserService = null!;
    private IMapper _mapper = null!;

    private IInventoryRepository _inventoryRepository = null!;
    private IStockMovementRepository _stockMovementRepository = null!;
    private ILowStockAlertRepository _lowStockAlertRepository = null!;
    private ILowStockAlertService _lowStockAlertService = null!;
    private IInventoryService _inventoryService = null!;
    
    private IPurchaseOrderRepository _purchaseOrderRepository = null!;
    private IRepository<Supplier> _supplierRepository = null!;
    private IPurchaseOrderService _purchaseOrderService = null!;

    private IWarehouseTransferRepository _transferRepository = null!;
    private IRepository<Category> _categoryRepository = null!;
    private IRepository<Company> _companyRepository = null!;
    private IProductService _productService = null!;
    private IWarehouseService _warehouseService = null!;
    private IWarehouseTransferService _warehouseTransferService = null!;

    private IWarehouseTaskService _warehouseTaskService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);

        // Repositories
        _warehouseTaskRepository = new WarehouseTaskRepository(_context);
        _warehouseRepository = new Repository<Warehouse>(_context);
        _productRepository = new ProductRepository(_context);
        _inventoryRepository = new InventoryRepository(_context);
        _stockMovementRepository = new StockMovementRepository(_context);
        _lowStockAlertRepository = new LowStockAlertRepository(_context);
        _purchaseOrderRepository = new PurchaseOrderRepository(_context);
        _supplierRepository = new Repository<Supplier>(_context);
        _transferRepository = new WarehouseTransferRepository(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);

        // Mock Current User Service
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(1);
        _mockCurrentUserService.Setup(x => x.Role).Returns("Admin");

        // Mapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        // Dependent Services
        _lowStockAlertService = new LowStockAlertService(_lowStockAlertRepository, _inventoryRepository, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<LowStockAlertService>>().Object, _mapper);
        _inventoryService = new InventoryService(_inventoryRepository, _productRepository, _warehouseRepository, _stockMovementRepository, _mockCurrentUserService.Object, _lowStockAlertService, _context, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<InventoryService>>().Object, _mapper);
        _purchaseOrderService = new PurchaseOrderService(_purchaseOrderRepository, _supplierRepository, _warehouseRepository, _productRepository, _mockCurrentUserService.Object, _context, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<PurchaseOrderService>>().Object, _mapper);
        _productService = new ProductService(_productRepository, _categoryRepository, _companyRepository, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<ProductService>>().Object, _mapper);
        _warehouseService = new WarehouseService(_warehouseRepository, _mockCurrentUserService.Object, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseService>>().Object, _mapper);
        _warehouseTransferService = new WarehouseTransferService(_transferRepository, _productService, _warehouseService, _warehouseRepository, _productRepository, _mockCurrentUserService.Object, _inventoryRepository, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseTransferService>>().Object, _mapper);

        // Warehouse Task Service Under Test
        _warehouseTaskService = new WarehouseTaskService(
            _warehouseTaskRepository,
            _warehouseRepository,
            _productRepository,
            _mockCurrentUserService.Object,
            _inventoryService,
            _purchaseOrderService,
            _warehouseTransferService,
            _purchaseOrderRepository,
            _transferRepository,
            _context,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseTaskService>>().Object,
            _mapper);

        // Seed default user for CreatedBy mappings
        var role = new Role { Id = 1, Name = "Admin" };
        _context.Roles.Add(role);

        var user = new User
        {
            Id = 1,
            Name = "Admin User",
            Email = "admin@test.com",
            RoleId = 1,
            Role = role,
            IsPasswordSet = true,
            PasswordHash = "hash"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helper Methods

    private async Task SeedBaseDataAsync()
    {
        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Warehouse 1",
            StorageType = StorageType.DryStorage,
            Capacity = 1000,
            AvailableCapacity = 1000,
            IsActive = true
        };
        await _warehouseRepository.AddAsync(warehouse);

        var category = new Category { Id = 1, Name = "Cat 1", IsActive = true };
        await _categoryRepository.AddAsync(category);

        var company = new Company { Id = 1, Name = "Company 1", IsActive = true };
        await _companyRepository.AddAsync(company);

        var product = new Product
        {
            Id = 1,
            Name = "Product 1",
            SKU = "PROD1",
            Barcode = "BAR1",
            CategoryId = 1,
            CompanyId = 1,
            RequiredStorageType = StorageType.DryStorage,
            Length = 1,
            Width = 1,
            Height = 1,
            IsActive = true
        };
        await _productRepository.AddAsync(product);
    }

    #endregion

    #region CreateTask Tests

    [Test]
    public async Task CreateTask_Success_StoreInventory()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Store custom items",
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        await _warehouseTaskService.CreateTaskAsync(request);

        var tasks = await _warehouseTaskRepository.GetTasksWithDetailsAsync();
        Assert.That(tasks, Has.Count.EqualTo(1));
        
        var task = tasks.First();
        Assert.That(task.Type, Is.EqualTo(WarehouseTaskType.StoreInventory));
        Assert.That(task.WarehouseId, Is.EqualTo(1));
        Assert.That(task.Status, Is.EqualTo(TaskStatusType.Pending));
        Assert.That(task.CreatedByUserId, Is.EqualTo(1));
        Assert.That(task.WarehouseTaskItems, Has.Count.EqualTo(1));
        Assert.That(task.WarehouseTaskItems.First().ProductId, Is.EqualTo(1));
        Assert.That(task.WarehouseTaskItems.First().Quantity, Is.EqualTo(10));
    }

    [Test]
    public async Task CreateTask_Success_WithPurchaseOrderReference()
    {
        await SeedBaseDataAsync();

        var supplier = new Supplier
        {
            Id = 1,
            Name = "Supplier 1",
            Email = "supp@test.com",
            PhoneNumber = "12345",
            Address = "Address 1",
            GSTNumber = "GST1",
            IsActive = true
        };
        await _supplierRepository.AddAsync(supplier);

        var po = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO-001",
            SupplierId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Received,
            CreatedByUserId = 1,
            PurchaseOrderItems = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem { ProductId = 1, OrderedQuantity = 25 }
            }
        };
        await _purchaseOrderRepository.AddAsync(po);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "PO task",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1
        };

        await _warehouseTaskService.CreateTaskAsync(request);

        var tasks = await _warehouseTaskRepository.GetTasksWithDetailsAsync();
        Assert.That(tasks, Has.Count.EqualTo(1));

        var task = tasks.First();
        Assert.That(task.ReferenceType, Is.EqualTo(WarehouseTaskReferenceType.PurchaseOrder));
        Assert.That(task.ReferenceId, Is.EqualTo(1));
        Assert.That(task.WarehouseTaskItems, Has.Count.EqualTo(1));
        Assert.That(task.WarehouseTaskItems.First().ProductId, Is.EqualTo(1));
        Assert.That(task.WarehouseTaskItems.First().Quantity, Is.EqualTo(25));
    }

    [Test]
    public async Task CreateTask_Success_WithWarehouseTransferReference()
    {
        await SeedBaseDataAsync();

        var destWh = new Warehouse
        {
            Id = 2,
            Name = "Warehouse 2",
            StorageType = StorageType.DryStorage,
            Capacity = 1000,
            AvailableCapacity = 1000,
            IsActive = true
        };
        await _warehouseRepository.AddAsync(destWh);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF-001",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Status = TransferStatus.Received,
            CreatedByUserId = 1
        };
        await _transferRepository.AddAsync(transfer);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 2, // Destination warehouse
            Description = "Transfer task",
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 1
        };

        await _warehouseTaskService.CreateTaskAsync(request);

        var tasks = await _warehouseTaskRepository.GetTasksWithDetailsAsync();
        Assert.That(tasks, Has.Count.EqualTo(1));

        var task = tasks.First();
        Assert.That(task.ReferenceType, Is.EqualTo(WarehouseTaskReferenceType.WarehouseTransfer));
        Assert.That(task.ReferenceId, Is.EqualTo(1));
        Assert.That(task.WarehouseId, Is.EqualTo(2));
    }

    [Test]
    public void CreateTask_WarehouseNotFound_ThrowsNotFound()
    {
        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 999,
            Description = "Invalid Warehouse"
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_InactiveWarehouse_ThrowsNotFound()
    {
        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Warehouse 1",
            IsActive = false
        };
        await _warehouseRepository.AddAsync(warehouse);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Inactive Warehouse"
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ForbiddenWarehouse_ThrowsForbidden()
    {
        await SeedBaseDataAsync();
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Forbidden Warehouse"
        };

        Assert.ThrowsAsync<ForbiddenException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ReferenceIdMissing_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Missing Ref Id",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ReferenceTypeMissing_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Missing Ref Type",
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_EmptyItemsAndNotReference_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "No items",
            Items = new List<CreateWarehouseTaskItemDto>()
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ItemsProvidedForReferenceTask_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Items with ref",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1,
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ReferenceTaskConflict_ThrowsConflict()
    {
        await SeedBaseDataAsync();

        var existingTask = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1,
            CreatedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(existingTask);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Conflict task",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<ConflictException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_PurchaseOrderNotFound_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "PO not found",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 999
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_PurchaseOrderWrongWarehouse_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        // Warehouse 2 is referenced in PO, seed it
        var wh2 = new Warehouse { Id = 2, Name = "Warehouse 2", StorageType = StorageType.DryStorage, Capacity = 1000, AvailableCapacity = 1000, IsActive = true };
        await _warehouseRepository.AddAsync(wh2);

        var supplier = new Supplier { Id = 1, Name = "Supplier 1", IsActive = true };
        await _supplierRepository.AddAsync(supplier);

        var po = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO-001",
            SupplierId = 1,
            WarehouseId = 2, // Different warehouse
            Status = PurchaseOrderStatus.Received,
            CreatedByUserId = 1
        };
        await _purchaseOrderRepository.AddAsync(po);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "PO wrong warehouse",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_PurchaseOrderNotReceived_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var supplier = new Supplier { Id = 1, Name = "Supplier 1", IsActive = true };
        await _supplierRepository.AddAsync(supplier);

        var po = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO-001",
            SupplierId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Ordered, // Not Received
            CreatedByUserId = 1
        };
        await _purchaseOrderRepository.AddAsync(po);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "PO not received",
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_WarehouseTransferNotFound_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Transfer not found",
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 999
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_WarehouseTransferWrongWarehouse_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        // Warehouse 3 is destination warehouse, seed it
        var wh3 = new Warehouse { Id = 3, Name = "Warehouse 3", StorageType = StorageType.DryStorage, Capacity = 1000, AvailableCapacity = 1000, IsActive = true };
        await _warehouseRepository.AddAsync(wh3);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF-001",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 3, // Destination is 3
            Status = TransferStatus.Approved,
            CreatedByUserId = 1
        };
        await _transferRepository.AddAsync(transfer);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1, // Selected warehouse is 1
            Description = "Wrong dest warehouse",
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_WarehouseTransferNotApproved_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF-001",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 1,
            Status = TransferStatus.Pending, // Not Approved
            CreatedByUserId = 1
        };
        await _transferRepository.AddAsync(transfer);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Transfer not approved",
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_DispatchOrderNotImplemented_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Dispatch order",
            ReferenceType = WarehouseTaskReferenceType.DispatchOrder,
            ReferenceId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_DuplicateProducts_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Duplicate products",
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 1, Quantity = 5 },
                new CreateWarehouseTaskItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_QuantityZeroOrLess_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Zero qty",
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 1, Quantity = 0 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ProductNotFound_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Invalid product",
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 999, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    [Test]
    public async Task CreateTask_ProductInactive_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var product = new Product
        {
            Id = 2,
            Name = "Product 2",
            SKU = "PROD2",
            Barcode = "BAR2",
            CategoryId = 1,
            CompanyId = 1,
            IsActive = false
        };
        await _productRepository.AddAsync(product);

        var request = new CreateWarehouseTaskDto
        {
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Description = "Inactive product",
            Items = new List<CreateWarehouseTaskItemDto>
            {
                new CreateWarehouseTaskItemDto { ProductId = 2, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CreateTaskAsync(request));
    }

    #endregion

    #region StartTask Tests

    [Test]
    public async Task StartTask_Success()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.Pending,
            CreatedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.StartTaskAsync(1);

        var updated = await _warehouseTaskRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TaskStatusType.InProgress));
        Assert.That(updated.StartedByUserId, Is.EqualTo(1));
        Assert.That(updated.StartedAt, Is.Not.Null);
    }

    [Test]
    public void StartTask_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.StartTaskAsync(999));
    }

    [Test]
    public async Task StartTask_InvalidStatus_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.Completed, // Not Pending
            CreatedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.StartTaskAsync(1));
    }

    #endregion

    #region CompleteTask Tests

    [Test]
    public async Task CompleteTask_Success_StoreInventory_NoReference()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1,
            WarehouseTaskItems = new List<WarehouseTaskItem>
            {
                new WarehouseTaskItem { ProductId = 1, Quantity = 10 }
            }
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.CompleteTaskAsync(1);

        var updated = await _warehouseTaskRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TaskStatusType.Completed));
        Assert.That(updated.CompletedByUserId, Is.EqualTo(1));
        Assert.That(updated.CompletedAt, Is.Not.Null);

        // Verify inventory was added
        var inventory = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory!.Quantity, Is.EqualTo(10));
    }

    [Test]
    public async Task CompleteTask_Success_RetrieveInventory_NoReference()
    {
        await SeedBaseDataAsync();

        // Seed initial inventory
        var inventory = new Inventory
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 50
        };
        await _inventoryRepository.AddAsync(inventory);

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.RetrieveInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1,
            WarehouseTaskItems = new List<WarehouseTaskItem>
            {
                new WarehouseTaskItem { ProductId = 1, Quantity = 10 }
            }
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.CompleteTaskAsync(1);

        var updated = await _warehouseTaskRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TaskStatusType.Completed));

        // Verify inventory was reduced
        var updatedInv = await _inventoryRepository.GetInventoryAsync(1, 1);
        Assert.That(updatedInv!.Quantity, Is.EqualTo(40));
    }

    [Test]
    public async Task CompleteTask_Success_StoreInventory_WithPurchaseOrder()
    {
        await SeedBaseDataAsync();

        var supplier = new Supplier { Id = 1, Name = "Supplier 1", IsActive = true };
        await _supplierRepository.AddAsync(supplier);

        var po = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO-001",
            SupplierId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Received,
            CreatedByUserId = 1
        };
        await _purchaseOrderRepository.AddAsync(po);

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1,
            ReferenceType = WarehouseTaskReferenceType.PurchaseOrder,
            ReferenceId = 1,
            WarehouseTaskItems = new List<WarehouseTaskItem>
            {
                new WarehouseTaskItem { ProductId = 1, Quantity = 10 }
            }
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.CompleteTaskAsync(1);

        var updatedPo = await _purchaseOrderRepository.GetByIdAsync(1);
        Assert.That(updatedPo!.Status, Is.EqualTo(PurchaseOrderStatus.Completed));
    }

    [Test]
    public async Task CompleteTask_Success_RetrieveInventory_WithWarehouseTransfer()
    {
        await SeedBaseDataAsync();

        // Seed inventory to retrieve from
        var inventory = new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 20 };
        await _inventoryRepository.AddAsync(inventory);

        // Warehouse 2 needs to exist as destination of transfer
        var wh2 = new Warehouse { Id = 2, Name = "Warehouse 2", StorageType = StorageType.DryStorage, Capacity = 1000, AvailableCapacity = 1000, IsActive = true };
        await _warehouseRepository.AddAsync(wh2);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF-001",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Status = TransferStatus.Approved,
            CreatedByUserId = 1
        };
        await _transferRepository.AddAsync(transfer);

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.RetrieveInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1,
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 1,
            WarehouseTaskItems = new List<WarehouseTaskItem>
            {
                new WarehouseTaskItem { ProductId = 1, Quantity = 10 }
            }
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.CompleteTaskAsync(1);

        var updatedTransfer = await _transferRepository.GetByIdAsync(1);
        Assert.That(updatedTransfer!.Status, Is.EqualTo(TransferStatus.InTransit));
    }

    [Test]
    public async Task CompleteTask_Success_StoreInventory_WithWarehouseTransfer()
    {
        await SeedBaseDataAsync();

        // Destination warehouse needs capacity reservations setup
        var destWh = await _warehouseRepository.GetByIdAsync(1);
        destWh!.ReservedCapacity = 100;
        await _warehouseRepository.UpdateAsync(destWh);

        // Warehouse 2 needs to exist as source of transfer
        var wh2 = new Warehouse { Id = 2, Name = "Warehouse 2", StorageType = StorageType.DryStorage, Capacity = 1000, AvailableCapacity = 1000, IsActive = true };
        await _warehouseRepository.AddAsync(wh2);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF-001",
            SourceWarehouseId = 2,
            DestinationWarehouseId = 1,
            Status = TransferStatus.Received,
            CreatedByUserId = 1,
            TransferVolume = 100
        };
        await _transferRepository.AddAsync(transfer);

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1,
            ReferenceType = WarehouseTaskReferenceType.WarehouseTransfer,
            ReferenceId = 1,
            WarehouseTaskItems = new List<WarehouseTaskItem>
            {
                new WarehouseTaskItem { ProductId = 1, Quantity = 10 }
            }
        };
        await _warehouseTaskRepository.AddAsync(task);

        await _warehouseTaskService.CompleteTaskAsync(1);

        var updatedTransfer = await _transferRepository.GetByIdAsync(1);
        Assert.That(updatedTransfer!.Status, Is.EqualTo(TransferStatus.Completed));
    }

    [Test]
    public void CompleteTask_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.CompleteTaskAsync(999));
    }

    [Test]
    public async Task CompleteTask_InvalidStatus_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.Pending, // Not InProgress
            CreatedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CompleteTaskAsync(1));
    }

    [Test]
    public async Task CompleteTask_NotStartedByUser_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 2 // Current user is 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        Assert.ThrowsAsync<BadRequestException>(async () => await _warehouseTaskService.CompleteTaskAsync(1));
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetTaskById_Success()
    {
        await SeedBaseDataAsync();

        var task = new WarehouseTask
        {
            Id = 1,
            Type = WarehouseTaskType.StoreInventory,
            WarehouseId = 1,
            Status = TaskStatusType.Pending,
            CreatedByUserId = 1,
            Description = "Task Details Test"
        };
        await _warehouseTaskRepository.AddAsync(task);

        var dto = await _warehouseTaskService.GetTaskByIdAsync(1);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Id, Is.EqualTo(1));
        Assert.That(dto.Description, Is.EqualTo("Task Details Test"));
        Assert.That(dto.WarehouseName, Is.EqualTo("Warehouse 1"));
        Assert.That(dto.CreatedBy, Is.EqualTo("Admin User"));
    }

    [Test]
    public void GetTaskById_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _warehouseTaskService.GetTaskByIdAsync(999));
    }

    [Test]
    public async Task GetTasks_PagedAndFiltered()
    {
        await SeedBaseDataAsync();

        var task1 = new WarehouseTask { Id = 1, Type = WarehouseTaskType.StoreInventory, WarehouseId = 1, Status = TaskStatusType.Pending, CreatedByUserId = 1, CreatedAt = DateTime.Now.AddMinutes(-2) };
        var task2 = new WarehouseTask { Id = 2, Type = WarehouseTaskType.RetrieveInventory, WarehouseId = 1, Status = TaskStatusType.InProgress, CreatedByUserId = 1, CreatedAt = DateTime.Now.AddMinutes(-1) };
        await _warehouseTaskRepository.AddAsync(task1);
        await _warehouseTaskRepository.AddAsync(task2);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new WarehouseTaskFilterDto { Type = WarehouseTaskType.StoreInventory };

        var result = await _warehouseTaskService.GetTasksAsync(pagination, filter);

        Assert.That(result.TotalRecords, Is.EqualTo(1));
        Assert.That(result.Data.First().Id, Is.EqualTo(1));

        // Filter by Status
        var filter2 = new WarehouseTaskFilterDto { Status = TaskStatusType.InProgress };
        var result2 = await _warehouseTaskService.GetTasksAsync(pagination, filter2);
        Assert.That(result2.TotalRecords, Is.EqualTo(1));
        Assert.That(result2.Data.First().Id, Is.EqualTo(2));

        // Filter by WarehouseId
        var filter3 = new WarehouseTaskFilterDto { WarehouseId = 1 };
        var result3 = await _warehouseTaskService.GetTasksAsync(pagination, filter3);
        Assert.That(result3.TotalRecords, Is.EqualTo(2));
    }

    [Test]
    public async Task GetTasks_WarehouseManager_RestrictsToAssignedWarehouse()
    {
        await SeedBaseDataAsync();

        // Add second warehouse
        var warehouse2 = new Warehouse { Id = 2, Name = "Warehouse 2", Capacity = 1000, AvailableCapacity = 1000, IsActive = true };
        await _warehouseRepository.AddAsync(warehouse2);

        var task1 = new WarehouseTask { Id = 10, Type = WarehouseTaskType.StoreInventory, WarehouseId = 1, Status = TaskStatusType.Pending, CreatedByUserId = 1, CreatedAt = DateTime.Now };
        var task2 = new WarehouseTask { Id = 20, Type = WarehouseTaskType.StoreInventory, WarehouseId = 2, Status = TaskStatusType.Pending, CreatedByUserId = 1, CreatedAt = DateTime.Now };
        await _warehouseTaskRepository.AddAsync(task1);
        await _warehouseTaskRepository.AddAsync(task2);

        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new WarehouseTaskFilterDto();

        var result = await _warehouseTaskService.GetTasksAsync(pagination, filter);
        Assert.That(result.TotalRecords, Is.EqualTo(1));
        Assert.That(result.Data.First().Id, Is.EqualTo(20));
    }

    #endregion

    #region Authorization Tests

    [Test]
    public async Task GetTaskById_OtherWarehouse_ThrowsForbidden()
    {
        await SeedBaseDataAsync();

        var wh2 = new Warehouse { Id = 2, Name = "Warehouse 2", IsActive = true };
        await _warehouseRepository.AddAsync(wh2);

        var task = new WarehouseTask
        {
            Id = 100,
            WarehouseId = 2,
            CreatedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(1);

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _warehouseTaskService.GetTaskByIdAsync(100));
    }

    [Test]
    public async Task CompleteTask_OtherWarehouse_ThrowsForbidden()
    {
        await SeedBaseDataAsync();

        var wh2 = new Warehouse { Id = 2, Name = "Warehouse 2", IsActive = true };
        await _warehouseRepository.AddAsync(wh2);

        var task = new WarehouseTask
        {
            Id = 100,
            WarehouseId = 2,
            Status = TaskStatusType.InProgress,
            CreatedByUserId = 1,
            StartedByUserId = 1
        };
        await _warehouseTaskRepository.AddAsync(task);

        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(1);

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _warehouseTaskService.CompleteTaskAsync(100));
    }

    #endregion
}
