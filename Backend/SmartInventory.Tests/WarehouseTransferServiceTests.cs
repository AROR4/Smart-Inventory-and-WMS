using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
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

namespace SmartInventory.Tests;

[TestFixture]
public class WarehouseTransferServiceTests
{
    private ApplicationDbContext _context = null!;
    private IWarehouseTransferRepository _transferRepository = null!;

    private IRepository<Role> _roleRepository = null!;
    private IRepository<Warehouse> _warehouseRepository = null!;
    private IProductRepository _productRepository = null!;
    private IInventoryRepository _inventoryRepository = null!;
    private IRepository<User> _userRepository = null!;
    private IRepository<Category> _categoryRepository = null!;
    private IRepository<Company> _companyRepository = null!;
    private Mock<ICurrentUserService> _mockCurrentUserService = null!;
    private IMapper _mapper = null!;
    
    private IProductService _productService = null!;
    private IWarehouseService _warehouseService = null!;
    private IWarehouseTransferService _transferService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _transferRepository = new WarehouseTransferRepository(_context);
        _warehouseRepository = new Repository<Warehouse>(_context);
        _productRepository = new ProductRepository(_context);
        _inventoryRepository = new InventoryRepository(_context);
        _userRepository = new Repository<User>(_context);
        _categoryRepository = new Repository<Category>(_context);
        _companyRepository = new Repository<Company>(_context);
        _roleRepository = new Repository<Role>(_context);

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(1);
        _mockCurrentUserService.Setup(x => x.Role).Returns("Admin");

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _productService = new ProductService(_productRepository, _categoryRepository, _companyRepository, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<ProductService>>().Object, _mapper);
        _warehouseService = new WarehouseService(_warehouseRepository, _mockCurrentUserService.Object, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseService>>().Object, _mapper);
        _transferService = new WarehouseTransferService(
            _transferRepository,
            _productService,
            _warehouseService,
            _warehouseRepository,
            _productRepository,
            _mockCurrentUserService.Object,
            _inventoryRepository,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseTransferService>>().Object,
            _mapper);

        // Seed default database entities
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
        _userRepository.AddAsync(user).Wait();

        var sourceWh = new Warehouse { Id = 1, Name = "Source WH", StorageType = StorageType.DryStorage, Capacity = 10000, AvailableCapacity = 10000, IsActive = true };
        var destWh = new Warehouse { Id = 2, Name = "Dest WH", StorageType = StorageType.DryStorage, Capacity = 10000, AvailableCapacity = 10000, IsActive = true };
        _warehouseRepository.AddAsync(sourceWh).Wait();
        _warehouseRepository.AddAsync(destWh).Wait();

        var category = new Category { Id = 1, Name = "Cat", IsActive = true };
        _categoryRepository.AddAsync(category).Wait();

        var company = new Company { Id = 1, Name = "Comp", IsActive = true };
        _companyRepository.AddAsync(company).Wait();

        var product = new Product
        {
            Id = 1,
            Name = "Prod",
            SKU = "SKU1",
            Barcode = "BAR1",
            CategoryId = 1,
            CompanyId = 1,
            Category = category,
            Company = company,
            RequiredStorageType = StorageType.DryStorage,
            Length = 2,
            Width = 2,
            Height = 2,
            IsActive = true
        };
        _productRepository.AddAsync(product).Wait();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateTransfer Tests

    [Test]
    public async Task CreateTransfer_Success()
    {
        var inventory = new Inventory { Id = 1, ProductId = 1, WarehouseId = 1, Quantity = 50 };
        await _inventoryRepository.AddAsync(inventory);

        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Reason = "Transfer test",
            Items = new List<CreateWarehouseTransferItemDto>
            {
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        await _transferService.CreateTransferAsync(request);

        var transfers = await _transferRepository.GetTransfersWithDetailsAsync();
        Assert.That(transfers, Has.Count.EqualTo(1));
        var transfer = transfers.First();
        Assert.That(transfer.SourceWarehouseId, Is.EqualTo(1));
        Assert.That(transfer.DestinationWarehouseId, Is.EqualTo(2));
        Assert.That(transfer.Status, Is.EqualTo(TransferStatus.Pending));
        Assert.That(transfer.TransferVolume, Is.EqualTo(80)); // 2*2*2 * 10 = 80
    }

    [Test]
    public void CreateTransfer_SameWarehouse_ThrowsBadRequest()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 1,
            Items = new List<CreateWarehouseTransferItemDto>
            {
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _transferService.CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_SourceWarehouseNotFound_ThrowsNotFound()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 999,
            DestinationWarehouseId = 2,
            Items = new List<CreateWarehouseTransferItemDto>
            {
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _transferService.CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_DuplicateProducts_ThrowsBadRequest()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items = new List<CreateWarehouseTransferItemDto>
            {
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 10 },
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 5 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _transferService.CreateTransferAsync(request));
    }

    [Test]
    public async Task CreateTransfer_IncompatibleStorageType_ThrowsBadRequest()
    {
        var coldWh = new Warehouse { Id = 3, Name = "Cold WH", StorageType = StorageType.ColdStorage, Capacity = 10000, AvailableCapacity = 10000, IsActive = true };
        await _warehouseRepository.AddAsync(coldWh);

        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 3,
            Items = new List<CreateWarehouseTransferItemDto>
            {
                new CreateWarehouseTransferItemDto { ProductId = 1, Quantity = 10 }
            }
        };

        Assert.ThrowsAsync<BadRequestException>(async () => await _transferService.CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_DestinationWarehouseNotFound_ThrowsNotFound()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 999,
            Items =
            [
                new CreateWarehouseTransferItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_NoItems_ThrowsBadRequest()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items = new List<CreateWarehouseTransferItemDto>()
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_ProductNotFound_ThrowsNotFound()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items =
            [
                new CreateWarehouseTransferItemDto
                {
                    ProductId = 999,
                    Quantity = 10
                }
            ]
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }

    [Test]
    public void CreateTransfer_InventoryNotFound_ThrowsBadRequest()
    {
        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items =
            [
                new CreateWarehouseTransferItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }

    [Test]
    public async Task CreateTransfer_InsufficientInventory_ThrowsBadRequest()
    {
        await _inventoryRepository.AddAsync(
            new Inventory
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 5
            });

        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items =
            [
                new CreateWarehouseTransferItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }

    [Test]
    public async Task CreateTransfer_InsufficientCapacity_ThrowsBadRequest()
    {
        var destWh =
            await _warehouseRepository
                .GetByIdAsync(2);

        destWh!.AvailableCapacity = 50;

        await _warehouseRepository
            .UpdateAsync(destWh);

        await _inventoryRepository.AddAsync(
            new Inventory
            {
                ProductId = 1,
                WarehouseId = 1,
                Quantity = 100
            });

        var request = new CreateWarehouseTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items =
            [
                new CreateWarehouseTransferItemDto
                {
                    ProductId = 1,
                    Quantity = 10
                }
            ]
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _transferService
                    .CreateTransferAsync(request));
    }


    #endregion

    #region ApproveTransfer Tests

    [Test]
    public async Task ApproveTransfer_Success()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending,
            TransferVolume = 300
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.ApproveTransferAsync(1);

        var updatedTransfer = await _transferRepository.GetByIdAsync(1);
        Assert.That(updatedTransfer!.Status, Is.EqualTo(TransferStatus.Approved));

        var updatedDestWh = await _warehouseRepository.GetByIdAsync(2);
        Assert.That(updatedDestWh!.ReservedCapacity, Is.EqualTo(300));
    }

    [Test]
    public async Task ApproveTransfer_InsufficientCapacity_ThrowsBadRequest()
    {
        var destWh = await _warehouseRepository.GetByIdAsync(2);
        destWh!.AvailableCapacity = 100;
        await _warehouseRepository.UpdateAsync(destWh);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending,
            TransferVolume = 300
        };
        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<BadRequestException>(async () => await _transferService.ApproveTransferAsync(1));
    }

    #endregion

    #region CompleteTransfer Tests

    [Test]
    public async Task CompleteTransfer_Success()
    {
        var destWh = await _warehouseRepository.GetByIdAsync(2);
        destWh!.ReservedCapacity = 300;
        await _warehouseRepository.UpdateAsync(destWh);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Received,
            TransferVolume = 300
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.CompleteTransferAsync(1);

        var updatedTransfer = await _transferRepository.GetByIdAsync(1);
        Assert.That(updatedTransfer!.Status, Is.EqualTo(TransferStatus.Completed));
        Assert.That(updatedTransfer.CompletedDate, Is.Not.Null);

        var updatedDestWh = await _warehouseRepository.GetByIdAsync(2);
        Assert.That(updatedDestWh!.ReservedCapacity, Is.EqualTo(0));
    }

    [Test]
    public void ApproveTransfer_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .ApproveTransferAsync(999));
    }

    [Test]
    public async Task ApproveTransfer_InvalidStatus_ThrowsConflict()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Approved,
            TransferVolume = 300
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .ApproveTransferAsync(1));
    }

    [Test]
    public async Task ApproveTransfer_DestinationWarehouseNotFound_ThrowsNotFound()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 999,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending,
            TransferVolume = 300
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .ApproveTransferAsync(1));
    }

    [Test]
    public void CompleteTransfer_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .CompleteTransferAsync(999));
    }

    [Test]
    public async Task CompleteTransfer_InvalidStatus_ThrowsConflict()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending,
            TransferVolume = 300
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .CompleteTransferAsync(1));
    }

    [Test]
    public async Task CompleteTransfer_ReservedCapacityGoesNegative_SetsZero()
    {
        var warehouse =
            await _warehouseRepository
                .GetByIdAsync(2);

        warehouse!.ReservedCapacity = 100;

        await _warehouseRepository
            .UpdateAsync(warehouse);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Received,
            TransferVolume = 300
        };

        await _transferRepository
            .AddAsync(transfer);

        await _transferService
            .CompleteTransferAsync(1);

        var updatedWarehouse =
            await _warehouseRepository
                .GetByIdAsync(2);

        Assert.That(
            updatedWarehouse!.ReservedCapacity,
            Is.EqualTo(0));
    }

    #endregion

    #region Reject & Cancel Transfer Tests

    [Test]
    public async Task RejectTransfer_Success()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.RejectTransferAsync(1, "Rejected reason");

        var updated = await _transferRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TransferStatus.Rejected));
        Assert.That(updated.RejectionReason, Is.EqualTo("Rejected reason"));
    }

    [Test]
    public async Task CancelTransfer_Success()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.CancelTransferAsync(1);

        var updated = await _transferRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TransferStatus.Cancelled));
    }

    [Test]
    public void RejectTransfer_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .RejectTransferAsync(
                        999,
                        "Rejected"));
    }

    [Test]
    public async Task RejectTransfer_InvalidStatus_ThrowsConflict()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Approved
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .RejectTransferAsync(
                        1,
                        "Rejected"));
    }

    [Test]
    public void CancelTransfer_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .CancelTransferAsync(
                        999));
    }

    [Test]
    public async Task CancelTransfer_InvalidStatus_ThrowsConflict()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Approved
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .CancelTransferAsync(
                        1));
    }

    #endregion

    #region Mark Status Tests

    [Test]
    public async Task MarkInTransit_Success()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Approved
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.MarkInTransitAsync(1);

        var updated = await _transferRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TransferStatus.InTransit));
    }

    [Test]
    public async Task MarkReceived_Success()
    {
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.InTransit
        };
        await _transferRepository.AddAsync(transfer);

        await _transferService.MarkReceivedAsync(1);

        var updated = await _transferRepository.GetByIdAsync(1);
        Assert.That(updated!.Status, Is.EqualTo(TransferStatus.Received));
    }

    [Test]
    public async Task MarkReceived_Forbidden_ThrowsForbidden()
    {
        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(3); // Dest is 2

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.InTransit
        };
        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ForbiddenException>(async () => await _transferService.MarkReceivedAsync(1));
    }

    [Test]
    public void MarkInTransit_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .MarkInTransitAsync(999));
    }

    [Test]
    public async Task MarkInTransit_InvalidStatus_ThrowsConflict()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .MarkInTransitAsync(1));
    }

    [Test]
    public void MarkReceived_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .MarkReceivedAsync(999));
    }

    [Test]
    public async Task MarkReceived_InvalidStatus_ThrowsConflict()
    {
        _mockCurrentUserService
            .Setup(x => x.AssignedWarehouseId)
            .Returns(2);

        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };

        await _transferRepository.AddAsync(transfer);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _transferService
                    .MarkReceivedAsync(1));
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetTransferById_Success()
    {
        var transfer = new WarehouseTransfer
        {
            Id = 1,
            TransferNumber = "TRF1",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };
        await _transferRepository.AddAsync(transfer);

        var result = await _transferService.GetTransferByIdAsync(1);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TransferNumber, Is.EqualTo("TRF1"));
    }


    [Test]
    public void GetTransferById_NotFound_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .GetTransferByIdAsync(999));
    }

    [Test]
    public void GetByTransferNumber_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _transferService
                    .GetByTransferNumberAsync(
                        "TR999"));
    }

    [Test]
    public async Task GetByTransferNumber_Success()
    {        
        var transfer = new WarehouseTransfer
        {
            TransferNumber = "TR001",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            CreatedByUserId = 1,
            Status = TransferStatus.Pending
        };

        await _transferRepository
            .AddAsync(transfer);

        var result =
            await _transferService
                .GetByTransferNumberAsync(
                    "TR001");

        Assert.That(
            result,
            Is.Not.Null);

        Assert.That(
            result.TransferNumber,
            Is.EqualTo("TR001"));
    }


    [Test]
    public async Task GetTransfers_Pagination()
    {

        for (int i = 1; i <= 3; i++)
        {
            await _transferRepository.AddAsync(
                new WarehouseTransfer
                {
                    TransferNumber = $"TR{i}",
                    SourceWarehouseId = 1,
                    DestinationWarehouseId = 2,
                    CreatedByUserId = 1,
                    Status = TransferStatus.Pending
                });
        }

        var result =
            await _transferService
                .GetTransfersAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 2
                    },
                    new WarehouseTransferFilterDto());

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(3));

        Assert.That(
            result.TotalPages,
            Is.EqualTo(2));

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(2));
    }

    [Test]
    public async Task GetTransfers_Filter()
    {

        await _transferRepository.AddAsync(
            new WarehouseTransfer
            {
                TransferNumber = "TR001",
                SourceWarehouseId = 1,
                DestinationWarehouseId = 2,
                CreatedByUserId = 1,
                Status = TransferStatus.Pending
            });

        await _transferRepository.AddAsync(
            new WarehouseTransfer
            {
                TransferNumber = "TR002",
                SourceWarehouseId = 2,
                DestinationWarehouseId = 1,
                CreatedByUserId = 1,
                Status = TransferStatus.Approved
            });

        var result =
            await _transferService
                .GetTransfersAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 10
                    },
                    new WarehouseTransferFilterDto
                    {
                        Status =
                            TransferStatus.Pending,

                        SourceWarehouseId = 1,

                        DestinationWarehouseId = 2
                    });

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(1));

        Assert.That(
            result.Data.Single()
                .TransferNumber,
            Is.EqualTo("TR001"));
    }

    #endregion

    #region Authorization Tests

    [Test]
    public async Task GetTransfers_WarehouseManager_ReturnsOnlyOwnWarehouseTransfers()
    {
        // Setup transfers
        var warehouse3 = new Warehouse { Id = 3, Name = "Warehouse 3", StorageType = StorageType.DryStorage, Capacity = 10000, AvailableCapacity = 10000, IsActive = true };
        await _warehouseRepository.AddAsync(warehouse3);

        var transfer1 = new WarehouseTransfer { Id = 10, TransferNumber = "TR-WH2-1", SourceWarehouseId = 1, DestinationWarehouseId = 2, CreatedByUserId = 1, Status = TransferStatus.Pending };
        var transfer2 = new WarehouseTransfer { Id = 20, TransferNumber = "TR-WH2-2", SourceWarehouseId = 2, DestinationWarehouseId = 3, CreatedByUserId = 1, Status = TransferStatus.Pending };
        var transfer3 = new WarehouseTransfer { Id = 30, TransferNumber = "TR-WH2-3", SourceWarehouseId = 1, DestinationWarehouseId = 3, CreatedByUserId = 1, Status = TransferStatus.Pending };

        await _transferRepository.AddAsync(transfer1);
        await _transferRepository.AddAsync(transfer2);
        await _transferRepository.AddAsync(transfer3);

        _mockCurrentUserService.Setup(x => x.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(x => x.AssignedWarehouseId).Returns(2);

        var result = await _transferService.GetTransfersAsync(
            new PaginationParams { PageNumber = 1, PageSize = 10 },
            new WarehouseTransferFilterDto());

        Assert.That(result.TotalRecords, Is.EqualTo(2));
        var transferNumbers = result.Data.Select(d => d.TransferNumber).ToList();
        Assert.That(transferNumbers, Contains.Item("TR-WH2-1"));
        Assert.That(transferNumbers, Contains.Item("TR-WH2-2"));
        Assert.That(transferNumbers, Does.Not.Contain("TR-WH2-3"));
    }

    #endregion
}
