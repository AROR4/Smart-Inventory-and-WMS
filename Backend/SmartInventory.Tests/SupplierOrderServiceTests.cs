using AutoMapper;
using Moq;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartInventory.Tests;

[TestFixture]
public class SupplierOrderServiceTests
{
    private ApplicationDbContext _context = null!;

    private IPurchaseOrderRepository _purchaseOrderRepository = null!;

    private IRepository<Supplier> _supplierRepository = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private IRepository<User> _userRepository = null!;

    private IRepository<Category> _categoryRepository = null!;

    private IRepository<Company> _companyRepository = null!;

    private IProductRepository _productRepository = null!;

    private Mock<ICurrentUserService> _mockCurrentUserService = null!;

    private IMapper _mapper = null!;

    private ISupplierOrderService _supplierOrderService = null!;

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
        _userRepository = new Repository<User>(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);
        _productRepository = new ProductRepository(_context);

        _mockCurrentUserService = new Mock<ICurrentUserService>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();

        _supplierOrderService = new SupplierOrderService(
            _purchaseOrderRepository,
            _mockCurrentUserService.Object,
            _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetPendingOrders Tests

    [Test]
    public void GetPendingOrders_SupplierNotSet()
    {
        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns((int?)null);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.GetPendingOrdersAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 10
                    }));
    }

    [Test]
    public async Task GetPendingOrders_Success()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order1 = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };
        var order2 = new PurchaseOrder
        {
            Id = 2,
            OrderNumber = "PO002",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 2000m,
            OrderedDate = DateTime.Now.AddDays(-1),
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order1);
        await _purchaseOrderRepository.AddAsync(order2);

        var result =
            await _supplierOrderService.GetPendingOrdersAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 10
                });

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.PageNumber, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.TotalRecords, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPendingOrders_Empty()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var result =
            await _supplierOrderService.GetPendingOrdersAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 10
                });

        Assert.That(result.Data, Is.Empty);
        Assert.That(result.TotalRecords, Is.EqualTo(0));
    }

    [Test]
    public async Task GetPendingOrders_Pagination()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        for (int i = 1; i <= 25; i++)
        {
            await _purchaseOrderRepository.AddAsync(new PurchaseOrder
            {
                Id = i,
                OrderNumber = $"PO{i:D3}",
                SupplierId = supplierId,
                Status = PurchaseOrderStatus.Ordered,
                TotalVolume = 1000m * i,
                OrderedDate = DateTime.Now,
                CreatedByUserId = 1,
                WarehouseId = 1
            });
        }

        var result =
            await _supplierOrderService.GetPendingOrdersAsync(
                new PaginationParams
                {
                    PageNumber = 2,
                    PageSize = 10
                });

        Assert.That(result.Data, Has.Count.EqualTo(10));
        Assert.That(result.PageNumber, Is.EqualTo(2));
        Assert.That(result.TotalRecords, Is.EqualTo(25));
        Assert.That(result.TotalPages, Is.EqualTo(3));
    }

    #endregion

    #region GetOrderHistory Tests

    [Test]
    public void GetOrderHistory_SupplierNotSet()
    {
        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns((int?)null);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.GetOrderHistoryAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 10
                    }));
    }

    [Test]
    public async Task GetOrderHistory_Success()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order1 = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Completed,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now.AddMonths(-1),
            ReceivedDate = DateTime.Now.AddDays(-10),
            CreatedByUserId = 1,
            WarehouseId = 1
        };
        var order2 = new PurchaseOrder
        {
            Id = 2,
            OrderNumber = "PO002",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Cancelled,
            TotalVolume = 500m,
            OrderedDate = DateTime.Now.AddMonths(-2),
            RejectionReason = "Out of stock",
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order1);
        await _purchaseOrderRepository.AddAsync(order2);

        var result =
            await _supplierOrderService.GetOrderHistoryAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 10
                });

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.TotalRecords, Is.EqualTo(2));
    }

    [Test]
    public async Task GetOrderHistory_Empty()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var result =
            await _supplierOrderService.GetOrderHistoryAsync(
                new PaginationParams
                {
                    PageNumber = 1,
                    PageSize = 10
                });

        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public async Task GetOrderHistory_WithPagination()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        for (int i = 1; i <= 50; i++)
        {
            await _purchaseOrderRepository.AddAsync(new PurchaseOrder
            {
                Id = i,
                OrderNumber = $"PO{i:D3}",
                SupplierId = supplierId,
                Status = PurchaseOrderStatus.Completed,
                TotalVolume = 1000m,
                OrderedDate = DateTime.Now.AddDays(-i),
                ReceivedDate = DateTime.Now.AddDays(-i + 5),
                CreatedByUserId = 1,
                WarehouseId = 1
            });
        }

        var result =
            await _supplierOrderService.GetOrderHistoryAsync(
                new PaginationParams
                {
                    PageNumber = 2,
                    PageSize = 20
                });

        Assert.That(result.Data, Has.Count.EqualTo(20));
        Assert.That(result.TotalRecords, Is.EqualTo(50));
        Assert.That(result.TotalPages, Is.EqualTo(3));
    }

    #endregion

    #region GetOrderDetails Tests

    [Test]
    public void GetOrderDetails_SupplierNotSet()
    {
        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns((int?)null);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.GetOrderDetailsAsync(1));
    }

    [Test]
    public void GetOrderDetails_OrderNotFound()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _supplierOrderService.GetOrderDetailsAsync(999));
    }

    [Test]
    public async Task GetOrderDetails_UnauthorizedSupplier()
    {
        var supplierId = 5;
        var otherSupplierId = 10;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = otherSupplierId, Name = "XYZ Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = otherSupplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.GetOrderDetailsAsync(1));
    }

    [Test]
    public async Task GetOrderDetails_Success()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
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
            IsActive = true
        };

        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);
        await _categoryRepository.AddAsync(category);
        await _companyRepository.AddAsync(company);
        await _productRepository.AddAsync(product);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1500m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1,
            InvoiceNumber = "INV001",
            PurchaseOrderItems = new List<PurchaseOrderItem>
            {
                new PurchaseOrderItem
                {
                    Id = 1,
                    PurchaseOrderId = 1,
                    ProductId = 1,
                    OrderedQuantity = 100,
                    UnitPrice = 15m
                }
            }
        };

        await _purchaseOrderRepository.AddAsync(order);

        var result =
            await _supplierOrderService.GetOrderDetailsAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.OrderNumber, Is.EqualTo("PO001"));
        Assert.That(result.TotalVolume, Is.EqualTo(1500m));
    }

    #endregion

    #region MarkOrderShipped Tests

    [Test]
    public void MarkOrderShipped_SupplierNotSet()
    {
        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns((int?)null);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.MarkOrderShippedAsync(1));
    }

    [Test]
    public void MarkOrderShipped_OrderNotFound()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _supplierOrderService.MarkOrderShippedAsync(999));
    }

    [Test]
    public async Task MarkOrderShipped_UnauthorizedSupplier()
    {
        var supplierId = 5;
        var otherSupplierId = 10;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = otherSupplierId, Name = "XYZ Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = otherSupplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ForbiddenException>(
            async () =>
                await _supplierOrderService.MarkOrderShippedAsync(1));
    }

    [Test]
    public async Task MarkOrderShipped_InvalidStatus()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Shipped,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _supplierOrderService.MarkOrderShippedAsync(1));
    }

    [Test]
    public async Task MarkOrderShipped_Success()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order);

        await _supplierOrderService.MarkOrderShippedAsync(1);

        var updated = await _purchaseOrderRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(PurchaseOrderStatus.Shipped));
    }

    [Test]
    public async Task MarkOrderShipped_UpdatesCalled()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order = new PurchaseOrder
        {
            Id = 5,
            OrderNumber = "PO005",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 5000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order);

        await _supplierOrderService.MarkOrderShippedAsync(5);

        var updated = await _purchaseOrderRepository.GetByIdAsync(5);
        Assert.That(updated!.Status, Is.EqualTo(PurchaseOrderStatus.Shipped));
    }

    [Test]
    public async Task MarkOrderShipped_MultipleOrders()
    {
        var supplierId = 5;

        _mockCurrentUserService
            .Setup(x => x.SupplierId)
            .Returns(supplierId);

        var supplier = new Supplier { Id = supplierId, Name = "ABC Supplies" };
        var warehouse = new Warehouse { Id = 1, Name = "Main Warehouse" };
        var user = new User { Id = 1, Name = "Admin User" };
        await _supplierRepository.AddAsync(supplier);
        await _warehouseRepository.AddAsync(warehouse);
        await _userRepository.AddAsync(user);

        var order1 = new PurchaseOrder
        {
            Id = 1,
            OrderNumber = "PO001",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 1000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        var order2 = new PurchaseOrder
        {
            Id = 2,
            OrderNumber = "PO002",
            SupplierId = supplierId,
            Status = PurchaseOrderStatus.Ordered,
            TotalVolume = 2000m,
            OrderedDate = DateTime.Now,
            CreatedByUserId = 1,
            WarehouseId = 1
        };

        await _purchaseOrderRepository.AddAsync(order1);
        await _purchaseOrderRepository.AddAsync(order2);

        await _supplierOrderService.MarkOrderShippedAsync(1);
        await _supplierOrderService.MarkOrderShippedAsync(2);

        var updated1 = await _purchaseOrderRepository.GetByIdAsync(1);
        var updated2 = await _purchaseOrderRepository.GetByIdAsync(2);
        Assert.That(updated1!.Status, Is.EqualTo(PurchaseOrderStatus.Shipped));
        Assert.That(updated2!.Status, Is.EqualTo(PurchaseOrderStatus.Shipped));
    }

    #endregion
}
