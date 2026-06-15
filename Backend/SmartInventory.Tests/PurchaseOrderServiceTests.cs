using AutoMapper;
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
using Moq;

namespace SmartInventory.Tests;

[TestFixture]
public class PurchaseOrderServiceTests
{
    private ApplicationDbContext _context = null!;

    private IPurchaseOrderRepository _purchaseOrderRepository = null!;

    private IRepository<Supplier> _supplierRepository = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private IProductRepository _productRepository = null!;

    private IRepository<Category> _categoryRepository = null!;

    private IRepository<Company> _companyRepository = null!;

    private IRepository<Role> _roleRepository = null!;

    private IRepository<User> _userRepository = null!;

    private Mock<ICurrentUserService> _mockCurrentUserService = null!;

    private IMapper _mapper = null!;

    private IPurchaseOrderService _purchaseOrderService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _purchaseOrderRepository = new PurchaseOrderRepository(_context);
        _supplierRepository = new Repository<Supplier>(_context);
        _warehouseRepository = new Repository<Warehouse>(_context);
        _productRepository = new ProductRepository(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);
        _roleRepository = new Repository<Role>(_context);
        _userRepository = new Repository<User>(_context);

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(1);
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(1);
        _mockCurrentUserService.Setup(x => x.Role).Returns("Admin");

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();

        _purchaseOrderService = new PurchaseOrderService(
            _purchaseOrderRepository,
            _supplierRepository,
            _warehouseRepository,
            _productRepository,
            _mockCurrentUserService.Object,
            _context,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<PurchaseOrderService>>().Object,
            _mapper);
    }

    #region CreatePurchaseOrder Tests

    [Test]
    public async Task CreatePurchaseOrder_Success()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 1000000,
            AvailableCapacity = 1000000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

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

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 100
                }
            }
        };

        await _purchaseOrderService.CreatePurchaseOrderAsync(request);

        var orders = await _purchaseOrderRepository.GetAllAsync();
        Assert.That(orders, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreatePurchaseOrder_SupplierNotFound()
    {
        var warehouse = new Warehouse
        {
            Id = 1,
            Name = "Main Warehouse",
            AddressLine1 = "123 St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _warehouseRepository.AddAsync(warehouse);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 999,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 100
                }
            }
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    [Test]
    public async Task CreatePurchaseOrder_WarehouseNotFound()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 999,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 100
                }
            }
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    [Test]
    public async Task CreatePurchaseOrder_NoItems()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>()
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    [Test]
    public async Task CreatePurchaseOrder_DuplicateProducts()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 100
                },
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 50
                }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    [Test]
    public async Task CreatePurchaseOrder_MultipleItems()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 1000000,
            AvailableCapacity = 1000000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var company = new Company { Id = 1, Name = "TechCorp", IsActive = true };

        var product1 = new Product
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

        var product2 = new Product
        {
            Id = 2,
            Name = "Mouse",
            SKU = "MOU001",
            Barcode = "BAR002",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 5,
            Width = 5,
            Height = 2,
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product1);
        await _productRepository.AddAsync(product2);

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 1,
                    Quantity = 100
                },
                new CreatePurchaseOrderItemDto
                {
                    ProductId = 2,
                    Quantity = 500
                }
            }
        };

        await _purchaseOrderService.CreatePurchaseOrderAsync(request);

        var orders = await _purchaseOrderRepository.GetAllAsync();
        Assert.That(orders, Has.Count.EqualTo(1));

        var order = orders.First();
        Assert.That(order.PurchaseOrderItems, Has.Count.EqualTo(2));
    }

    #endregion

    #region GetPurchaseOrder Tests

    [Test]
    public async Task GetPurchaseOrder_ById()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var role = new Role { Id = 1, Name = "Admin" };
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

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            Supplier = supplier,
            CreatedByUserId = 1,
            CreatedByUser = user,
            WarehouseId = 1,
            Warehouse = warehouse,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
        await _purchaseOrderRepository.AddAsync(order);

        var result = await _purchaseOrderService.GetPurchaseOrderByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.OrderNumber, Is.EqualTo("PO001"));
    }

    [Test]
    public void GetPurchaseOrder_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.GetPurchaseOrderByIdAsync(999));
    }

    #endregion

    #region ReceivePurchaseOrder Tests

    [Test]
    public async Task ReceivePurchaseOrder_Success()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var role = new Role { Id = 1, Name = "Admin" };
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

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            Supplier = supplier,
            CreatedByUserId = 1,
            CreatedByUser = user,
            WarehouseId = 1,
            Warehouse = warehouse,
            Status = PurchaseOrderStatus.Shipped,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
        await _purchaseOrderRepository.AddAsync(order);

        var request = new ReceivePurchaseOrderDto
        {
            InvoiceNumber = "INV001"
        };

        await _purchaseOrderService.ReceivePurchaseOrderAsync(1, "INV001");

        var updated = await _purchaseOrderRepository.GetByIdAsync(1);
        Assert.That(updated.Status, Is.EqualTo(PurchaseOrderStatus.Received));
    }

    [Test]
    public async Task ReceivePurchaseOrder_InvalidStatus()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var role = new Role { Id = 1, Name = "Admin" };
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

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            Supplier = supplier,
            CreatedByUserId = 1,
            CreatedByUser = user,
            WarehouseId = 1,
            Warehouse = warehouse,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
        await _purchaseOrderRepository.AddAsync(order);

        var request = new ReceivePurchaseOrderDto
        {
            InvoiceNumber = "INV001"
        };

        Assert.ThrowsAsync<ConflictException>(
            async () => await _purchaseOrderService.ReceivePurchaseOrderAsync(1, "INV001"));
    }

    #endregion

    #region RejectPurchaseOrder Tests

    [Test]
    public async Task RejectPurchaseOrder_Success()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var role = new Role { Id = 1, Name = "Admin" };
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

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            Supplier = supplier,
            CreatedByUserId = 1,
            CreatedByUser = user,
            WarehouseId = 1,
            Warehouse = warehouse,
            Status = PurchaseOrderStatus.PendingApproval,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
        await _purchaseOrderRepository.AddAsync(order);

        var request = new RejectPurchaseOrderDto
        {
            Reason = "Quality issues"
        };

        await _purchaseOrderService.RejectPurchaseOrderAsync(1, "Quality issues");

        var updated = await _purchaseOrderRepository.GetByIdAsync(1);
        Assert.That(updated.Status, Is.EqualTo(PurchaseOrderStatus.Rejected));
        Assert.That(updated.RejectionReason, Is.EqualTo("Quality issues"));
    }

    [Test]
    public async Task RejectPurchaseOrder_InvalidStatus()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Supplies",
            Email = "abc@test.com",
            PhoneNumber = "1234567890",
            Address = "123 Supplier St",
            GSTNumber = "GST123",
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
            Capacity = 10000,
            AvailableCapacity = 10000,
            StorageType = StorageType.DryStorage,
            IsActive = true
        };

        var role = new Role { Id = 1, Name = "Admin" };
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

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            Supplier = supplier,
            CreatedByUserId = 1,
            CreatedByUser = user,
            WarehouseId = 1,
            Warehouse = warehouse,
            Status = PurchaseOrderStatus.Received,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
        await _purchaseOrderRepository.AddAsync(order);

        var request = new RejectPurchaseOrderDto
        {
            Reason = "No longer needed"
        };

        Assert.ThrowsAsync<ConflictException>(
            async () => await _purchaseOrderService.RejectPurchaseOrderAsync(1, "No longer needed"));
    }

    #endregion

    #region Helper Methods

    private async Task SeedBaseDataAsync()
    {
        var supplier = new Supplier { Id = 1, Name = "ABC Supplies", Email = "abc@test.com", GSTNumber = "GST123", IsActive = true };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse", Capacity = 10000, AvailableCapacity = 10000, StorageType = StorageType.DryStorage, IsActive = true };
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
        var role = new Role { Id = 1, Name = "Admin" };
        var user = new User { Id = 1, Name = "Admin User", Email = "admin@test.com", RoleId = 1, Role = role, IsPasswordSet = true, PasswordHash = "hash" };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);
        await _roleRepository.AddAsync(role);
        await _userRepository.AddAsync(user);
    }

    #endregion

    #region CreatePurchaseOrder Tests

    [Test]
    public async Task CreatePurchaseOrder_ProductNotFound()
    {
        await SeedBaseDataAsync();

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto { ProductId = 999, Quantity = 5 } // Product 999 does not exist
            }
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    [Test]
    public async Task CreatePurchaseOrder_AdminRole_Success_WithCapacityCheck()
    {
        await SeedBaseDataAsync();
        _mockCurrentUserService.Setup(x => x.Role).Returns("Admin");

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto { ProductId = 1, Quantity = 10 } // Vol: 10 * 20 * 2 * 10 = 4000
            }
        };

        await _purchaseOrderService.CreatePurchaseOrderAsync(request);

        var orders = await _purchaseOrderRepository.GetAllAsync();
        Assert.That(orders, Has.Count.EqualTo(1));
        var order = orders.First();
        Assert.That(order.Status, Is.EqualTo(PurchaseOrderStatus.Ordered));
        Assert.That(order.TotalVolume, Is.EqualTo(4000));

        var warehouse = await _warehouseRepository.GetByIdAsync(1);
        Assert.That(warehouse.ReservedCapacity, Is.EqualTo(4000));
    }

    [Test]
    public async Task CreatePurchaseOrder_AdminRole_InsufficientCapacity_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();
        _mockCurrentUserService.Setup(x => x.Role).Returns("Admin");

        var request = new CreatePurchaseOrderDto
        {
            SupplierId = 1,
            WarehouseId = 1,
            Items = new List<CreatePurchaseOrderItemDto>
            {
                new CreatePurchaseOrderItemDto { ProductId = 1, Quantity = 50 } // Vol: 10 * 20 * 2 * 50 = 20000 (exceeds 10000 capacity)
            }
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _purchaseOrderService.CreatePurchaseOrderAsync(request));
    }

    #endregion

    #region ApprovePurchaseOrder Tests

    [Test]
    public async Task ApprovePurchaseOrder_Success()
    {
        await SeedBaseDataAsync();

         var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.PendingApproval,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };

        await _purchaseOrderRepository.AddAsync(order);
        
        await _purchaseOrderService.ApprovePurchaseOrderAsync(order.Id);

        var updated = await _purchaseOrderRepository.GetByIdAsync(order.Id);
        Assert.That(updated.Status, Is.EqualTo(PurchaseOrderStatus.Ordered));

        var warehouse = await _warehouseRepository.GetByIdAsync(order.WarehouseId);
        Assert.That(warehouse.ReservedCapacity, Is.EqualTo(3000));
    }

    [Test]
    public void ApprovePurchaseOrder_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.ApprovePurchaseOrderAsync(999));
    }

    [Test]
    public async Task ApprovePurchaseOrder_InvalidStatus_ThrowsConflict()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Ordered, // Not PendingApproval
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ConflictException>(
            async () => await _purchaseOrderService.ApprovePurchaseOrderAsync(1));
    }

    [Test]
    public async Task ApprovePurchaseOrder_WarehouseNotFound_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 99,
            Status = PurchaseOrderStatus.PendingApproval,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };

        await _purchaseOrderRepository.AddAsync(order);

       
        var ex = Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _purchaseOrderService
                    .ApprovePurchaseOrderAsync(order.Id));

        Assert.That(
            ex!.Message,
            Is.EqualTo("Warehouse not found."));
    }

    [Test]
    public async Task ApprovePurchaseOrder_InsufficientCapacity_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.PendingApproval,
            TotalVolume = 25000m, // Exceeds 10000 capacity
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _purchaseOrderService.ApprovePurchaseOrderAsync(1));
    }

    #endregion

    #region RejectPurchaseOrder Tests

    [Test]
    public void RejectPurchaseOrder_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.RejectPurchaseOrderAsync(999, "No PO"));
    }

    #endregion

    #region Uncovered Paths - ReceivePurchaseOrder Tests

    [Test]
    public void ReceivePurchaseOrder_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.ReceivePurchaseOrderAsync(999, "INV-123"));
    }

    [Test]
    public async Task ReceivePurchaseOrder_InvoiceNumberNullOrWhiteSpace_ThrowsBadRequest()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Shipped,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<BadRequestException>(
            async () => await _purchaseOrderService.ReceivePurchaseOrderAsync(1, "   "));
    }

    [Test]
    public async Task ReceivePurchaseOrder_ForbiddenWarehouse_ThrowsForbidden()
    {
        await SeedBaseDataAsync();
        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2); // Current user's assigned WH is 2, order is WH 1

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Shipped,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _purchaseOrderService.ReceivePurchaseOrderAsync(1, "INV-123"));
    }

    #endregion

    #region Uncovered Paths - GetByOrderNumber Tests

    [Test]
    public async Task GetByOrderNumber_Success()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO-SPECIAL-99",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        var result = await _purchaseOrderService.GetByOrderNumberAsync("PO-SPECIAL-99");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.OrderNumber, Is.EqualTo("PO-SPECIAL-99"));
    }

    [Test]
    public void GetByOrderNumber_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.GetByOrderNumberAsync("NON-EXISTENT"));
    }

    #endregion

    #region Uncovered Paths - CompletePurchaseOrder Tests

    [Test]
    public async Task CompletePurchaseOrder_Success()
    {
        await SeedBaseDataAsync();

        var warehouse = await _warehouseRepository.GetByIdAsync(1);
        warehouse.ReservedCapacity = 5000m;
        await _warehouseRepository.UpdateAsync(warehouse);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Received,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        await _purchaseOrderService.CompletePurchaseOrderAsync(1);

        var updatedOrder = await _purchaseOrderRepository.GetByIdAsync(1);
        Assert.That(updatedOrder.Status, Is.EqualTo(PurchaseOrderStatus.Completed));

        var updatedWh = await _warehouseRepository.GetByIdAsync(1);
        Assert.That(updatedWh.ReservedCapacity, Is.EqualTo(2000));
    }

    [Test]
    public async Task CompletePurchaseOrder_Success_CapLessThanZero()
    {
        await SeedBaseDataAsync();

        var warehouse = await _warehouseRepository.GetByIdAsync(1);
        warehouse.ReservedCapacity = 1000m; // Reserved is less than order volume
        await _warehouseRepository.UpdateAsync(warehouse);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Received,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        await _purchaseOrderService.CompletePurchaseOrderAsync(1);

        var updatedWh = await _warehouseRepository.GetByIdAsync(1);
        Assert.That(updatedWh.ReservedCapacity, Is.EqualTo(0)); // Reset to 0 since 1000 - 3000 < 0
    }

    [Test]
    public void CompletePurchaseOrder_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.CompletePurchaseOrderAsync(999));
    }

    [Test]
    public async Task CompletePurchaseOrder_InvalidStatus_ThrowsConflict()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Ordered, // Not Received
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ConflictException>(
            async () => await _purchaseOrderService.CompletePurchaseOrderAsync(1));
    }

    [Test]
    public async Task CompletePurchaseOrder_WarehouseNotFound_ThrowsNotFound()
    {
        await SeedBaseDataAsync();

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = 1,
            CreatedByUserId = 1,
            WarehouseId = 999, // Warehouse 999 doesn't exist
            Status = PurchaseOrderStatus.Received,
            TotalVolume = 3000m,
            OrderedDate = DateTime.Now
        };
        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<NotFoundException>(
            async () => await _purchaseOrderService.CompletePurchaseOrderAsync(1));
    }

    #endregion

    #region Uncovered Paths - GetPurchaseOrders Tests

    [Test]
    public async Task GetPurchaseOrders_PagedAndFiltered()
    {
        await SeedBaseDataAsync();

        var order1 = new PurchaseOrder { Id = 1, OrderNumber = "PO-FILTER-1", SupplierId = 1, WarehouseId = 1, Status = PurchaseOrderStatus.Ordered, CreatedByUserId = 1, OrderedDate = DateTime.Now.AddDays(-2) };
        var order2 = new PurchaseOrder { Id = 2, OrderNumber = "PO-FILTER-2", SupplierId = 1, WarehouseId = 1, Status = PurchaseOrderStatus.PendingApproval, CreatedByUserId = 1, OrderedDate = DateTime.Now.AddDays(-1) };
        await _purchaseOrderRepository.AddAsync(order1);
        await _purchaseOrderRepository.AddAsync(order2);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PurchaseOrderFilterDto { Status = PurchaseOrderStatus.Ordered };

        var result = await _purchaseOrderService.GetPurchaseOrdersAsync(pagination, filter);
        Assert.That(result.TotalRecords, Is.EqualTo(1));
        Assert.That(result.Data.First().OrderNumber, Is.EqualTo("PO-FILTER-1"));

        // Supplier filter
        var filter2 = new PurchaseOrderFilterDto { SupplierId = 1 };
        var result2 = await _purchaseOrderService.GetPurchaseOrdersAsync(pagination, filter2);
        Assert.That(result2.TotalRecords, Is.EqualTo(2));

        // Warehouse filter
        var filter3 = new PurchaseOrderFilterDto { WarehouseId = 1 };
        var result3 = await _purchaseOrderService.GetPurchaseOrdersAsync(pagination, filter3);
        Assert.That(result3.TotalRecords, Is.EqualTo(2));

        // Search filter
        var filter4 = new PurchaseOrderFilterDto { Search = "PO-FILTER-2" };
        var result4 = await _purchaseOrderService.GetPurchaseOrdersAsync(pagination, filter4);
        Assert.That(result4.TotalRecords, Is.EqualTo(1));
        Assert.That(result4.Data.First().OrderNumber, Is.EqualTo("PO-FILTER-2"));
    }

    #endregion

    #region Authorization Tests

    [Test]
    public async Task GetPurchaseOrders_WarehouseManager_ReturnsOnlyOwnWarehouseOrders()
    {
        await SeedBaseDataAsync();

        // Add second warehouse
        var warehouse2 = new Warehouse { Id = 2, Name = "Secondary Warehouse", Capacity = 10000, AvailableCapacity = 10000, StorageType = StorageType.DryStorage, IsActive = true };
        await _warehouseRepository.AddAsync(warehouse2);

        var order1 = new PurchaseOrder { Id = 1, OrderNumber = "PO-WH1", SupplierId = 1, WarehouseId = 1, Status = PurchaseOrderStatus.Ordered, CreatedByUserId = 1, OrderedDate = DateTime.Now };
        var order2 = new PurchaseOrder { Id = 2, OrderNumber = "PO-WH2", SupplierId = 1, WarehouseId = 2, Status = PurchaseOrderStatus.Ordered, CreatedByUserId = 1, OrderedDate = DateTime.Now };
        await _purchaseOrderRepository.AddAsync(order1);
        await _purchaseOrderRepository.AddAsync(order2);

        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PurchaseOrderFilterDto();

        var result = await _purchaseOrderService.GetPurchaseOrdersAsync(pagination, filter);
        Assert.That(result.TotalRecords, Is.EqualTo(1));
        Assert.That(result.Data.First().OrderNumber, Is.EqualTo("PO-WH2"));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
