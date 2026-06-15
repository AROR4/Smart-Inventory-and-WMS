using AutoMapper;
using Moq;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.BusinessLayer.Mappings;

namespace SmartInventory.Tests;

[TestFixture]
public class WarehouseServiceTests
{
    private ApplicationDbContext _context = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private IWarehouseService _warehouseService = null!;

    private IMapper _mapper = null!;

    private Mock<ICurrentUserService> _mockCurrentUserService = null!;

    [SetUp]
    public void Setup()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        _context =
            new ApplicationDbContext(options);

        _warehouseRepository =
            new Repository<Warehouse>(_context);

        var mapperConfig =
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

        _mapper = mapperConfig.CreateMapper();

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(m => m.Role).Returns("Admin");

        _warehouseService =
            new WarehouseService(
                _warehouseRepository,
                _mockCurrentUserService.Object,
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<WarehouseService>>().Object,
                _mapper);
    }

    #region CreateWarehouse Tests

    [Test]
    public async Task CreateWarehouse_Success()
    {
        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Main Warehouse",
                AddressLine1 = "123 Storage St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 10000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        Assert.That(
            warehouses,
            Has.Count.EqualTo(1));

        var createdWarehouse =
            warehouses.First();

        Assert.That(
            createdWarehouse.Name,
            Is.EqualTo("Main Warehouse"));

        Assert.That(
            createdWarehouse.AvailableCapacity,
            Is.EqualTo(10000m));

        Assert.That(
            createdWarehouse.IsActive,
            Is.True);
    }

    [Test]
    public async Task CreateWarehouse_CapacityInitialization()
    {
        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Secondary Warehouse",
                AddressLine1 = "456 Storage Ave",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 5000,
                StorageType = StorageType.ColdStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        var warehouse = warehouses.First();

        Assert.That(
            warehouse.Capacity,
            Is.EqualTo(5000m));

        Assert.That(
            warehouse.AvailableCapacity,
            Is.EqualTo(5000m));

        Assert.That(
            warehouse.ReservedCapacity,
            Is.EqualTo(0));
    }

    [Test]
    public async Task CreateWarehouse_DuplicateName()
    {
        var warehouse1 =
            new Warehouse
            {
                Name = "Main Warehouse",
                AddressLine1 = "123 Storage St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 10000,
                AvailableCapacity = 10000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse1);

        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Main Warehouse",
                AddressLine1 = "789 New St",
                City = "Bangalore",
                State = "Karnataka",
                PostalCode = "560001",
                Capacity = 5000,
                StorageType = StorageType.DryStorage
            };

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _warehouseService.CreateWarehouseAsync(createRequest));
    }

    [Test]
    public async Task CreateWarehouse_WithOptionalAddressLine2()
    {
        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Premium Warehouse",
                AddressLine1 = "999 Storage Blvd",
                AddressLine2 = "Building A",
                City = "Pune",
                State = "Maharashtra",
                PostalCode = "411001",
                Capacity = 7500,
                StorageType = StorageType.HazardousStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        var warehouse = warehouses.First();

        Assert.That(
            warehouse.AddressLine2,
            Is.EqualTo("Building A"));
    }

    [Test]
    public async Task CreateWarehouse_MultipleWarehouses()
    {
        var createRequest1 =
            new CreateWarehouseDto
            {
                Name = "Warehouse A",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                StorageType = StorageType.DryStorage
            };

        var createRequest2 =
            new CreateWarehouseDto
            {
                Name = "Warehouse B",
                AddressLine1 = "456 Ave",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 7000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest1);
        await _warehouseService.CreateWarehouseAsync(createRequest2);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        Assert.That(
            warehouses,
            Has.Count.EqualTo(2));
    }

    #endregion

    #region GetWarehouseById Tests

    [Test]
    public async Task GetWarehouseById_Success()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Main Warehouse",
                AddressLine1 = "123 Storage St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 10000,
                AvailableCapacity = 10000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var result =
            await _warehouseService.GetWarehouseByIdAsync(1);

        Assert.That(
            result.Name,
            Is.EqualTo("Main Warehouse"));

        Assert.That(
            result.City,
            Is.EqualTo("Delhi"));

        Assert.That(
            result.Capacity,
            Is.EqualTo(10000m));
    }

    [Test]
    public void GetWarehouseById_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _warehouseService.GetWarehouseByIdAsync(999));
    }

    [Test]
    public async Task GetWarehouseById_ReturnsCorrectDTO()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                AddressLine2 = "Floor 2",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 5000,
                AvailableCapacity = 3000,
                ReservedCapacity = 2000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var result =
            await _warehouseService.GetWarehouseByIdAsync(1);

        Assert.That(
            result,
            Is.Not.Null);

        Assert.That(
            result.AvailableCapacity,
            Is.EqualTo(3000m));

        Assert.That(
            result.ReservedCapacity,
            Is.EqualTo(2000m));
    }

    [Test]
    public async Task GetWarehouseById_WarehouseManager_AccessesOwnWarehouse_Success()
    {
        var warehouse = new Warehouse
        {
            Id = 10,
            Name = "Main Warehouse",
            AddressLine1 = "123 Storage St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 10000,
            AvailableCapacity = 10000,
            IsActive = true
        };

        await _warehouseRepository.AddAsync(warehouse);

        _mockCurrentUserService.Setup(m => m.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(m => m.AssignedWarehouseId).Returns(10);

        var result = await _warehouseService.GetWarehouseByIdAsync(10);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Main Warehouse"));
    }

    [Test]
    public async Task GetWarehouseById_WarehouseManager_AccessesDifferentWarehouse_ThrowsForbiddenException()
    {
        var warehouse = new Warehouse
        {
            Id = 11,
            Name = "Main Warehouse",
            AddressLine1 = "123 Storage St",
            City = "Delhi",
            State = "Delhi",
            PostalCode = "110001",
            Capacity = 10000,
            AvailableCapacity = 10000,
            IsActive = true
        };

        await _warehouseRepository.AddAsync(warehouse);

        _mockCurrentUserService.Setup(m => m.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(m => m.AssignedWarehouseId).Returns(20); // different warehouse

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _warehouseService.GetWarehouseByIdAsync(11));
    }

    #endregion

    #region GetAllWarehouses Tests

    [Test]
    public async Task GetAllWarehouses_Empty()
    {
        var result =
            await _warehouseService.GetAllWarehousesAsync();

        Assert.That(
            result,
            Is.Empty);
    }

    [Test]
    public async Task GetAllWarehouses_Success()
    {
        var warehouse1 =
            new Warehouse
            {
                Name = "Warehouse 1",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        var warehouse2 =
            new Warehouse
            {
                Name = "Warehouse 2",
                AddressLine1 = "456 Ave",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 8000,
                AvailableCapacity = 6000,
                
                IsActive = true
            };

        var warehouse3 =
            new Warehouse
            {
                Name = "Warehouse 3",
                AddressLine1 = "789 Rd",
                City = "Bangalore",
                State = "Karnataka",
                PostalCode = "560001",
                Capacity = 10000,
                AvailableCapacity = 10000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse1);
        await _warehouseRepository.AddAsync(warehouse2);
        await _warehouseRepository.AddAsync(warehouse3);

        var result =
            await _warehouseService.GetAllWarehousesAsync();

        Assert.That(
            result,
            Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetAllWarehouses_ReturnsCorrectDTOs()
    {
        var warehouse =
            new Warehouse
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 3000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var result =
            await _warehouseService.GetAllWarehousesAsync();

        var warehouseResult = result.First();

        Assert.That(
            warehouseResult.Name,
            Is.EqualTo("Test Warehouse"));

        Assert.That(
            warehouseResult.AvailableCapacity,
            Is.EqualTo(3000m));
    }

    #endregion

    #region UpdateWarehouse Tests

    [Test]
    public async Task UpdateWarehouse_Success()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Original Name",
                AddressLine1 = "123 Original St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Updated Name",
                AddressLine1 = "456 New St",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 5000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            updatedWarehouse!.Name,
            Is.EqualTo("Updated Name"));

        Assert.That(
            updatedWarehouse.AddressLine1,
            Is.EqualTo("456 New St"));

        Assert.That(
            updatedWarehouse.City,
            Is.EqualTo("Mumbai"));
    }

    [Test]
    public void UpdateWarehouse_NotFound()
    {
        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Updated Name",
                AddressLine1 = "456 New St",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 5000,
                StorageType = StorageType.DryStorage
            };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _warehouseService.UpdateWarehouseAsync(999, updateRequest));
    }

    [Test]
    public async Task UpdateWarehouse_IncreaseCapacity()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 8000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            updatedWarehouse!.Capacity,
            Is.EqualTo(8000m));

        Assert.That(
            updatedWarehouse.AvailableCapacity,
            Is.EqualTo(8000m));
    }

    [Test]
    public async Task UpdateWarehouse_DecreaseCapacity()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 10000,
                AvailableCapacity = 8000,
                ReservedCapacity = 2000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            updatedWarehouse!.Capacity,
            Is.EqualTo(5000m));

        Assert.That(
            updatedWarehouse.AvailableCapacity,
            Is.EqualTo(3000m));
    }

    [Test]
    public async Task UpdateWarehouse_DecreaseCapacityBelowReserved()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 10000,
                AvailableCapacity = 7000,
                ReservedCapacity = 3000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 2000,
                StorageType = StorageType.DryStorage
            };

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _warehouseService.UpdateWarehouseAsync(1, updateRequest));
    }

    [Test]
    public async Task UpdateWarehouse_CapacityRecalculation()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 3000,
                ReservedCapacity = 2000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 7000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            updatedWarehouse!.Capacity,
            Is.EqualTo(7000m));

        Assert.That(
            updatedWarehouse.AvailableCapacity,
            Is.EqualTo(5000m));
    }

    [Test]
    public async Task UpdateWarehouse_StorageTypeChange()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                StorageType = StorageType.ColdStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            updatedWarehouse!.StorageType,
            Is.EqualTo(StorageType.ColdStorage));
    }

    #endregion

    #region DeleteWarehouse Tests

    [Test]
    public async Task DeleteWarehouse_Success()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        await _warehouseService.DeleteWarehouseAsync(1);

        var deletedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            deletedWarehouse!.IsActive,
            Is.False);
    }

    [Test]
    public void DeleteWarehouse_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _warehouseService.DeleteWarehouseAsync(999));
    }

    [Test]
    public async Task DeleteWarehouse_WithInventory()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 3000,
                ReservedCapacity = 2000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _warehouseService.DeleteWarehouseAsync(1));
    }

    [Test]
    public async Task DeleteWarehouse_EmptyWarehouse()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                ReservedCapacity = 0,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        await _warehouseService.DeleteWarehouseAsync(1);

        var deletedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            deletedWarehouse!.IsActive,
            Is.False);
    }

    [Test]
    public async Task DeleteWarehouse_MarksAsInactive()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Test Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        Assert.That(
            warehouse.IsActive,
            Is.True);

        await _warehouseService.DeleteWarehouseAsync(1);

        var result =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(
            result!.IsActive,
            Is.False);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public async Task CreateWarehouse_SmallCapacity()
    {
        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Micro Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 0.0001m,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        var warehouse = warehouses.First();

        Assert.That(
            warehouse.Capacity,
            Is.EqualTo(0.0001m));
    }

    [Test]
    public async Task CreateWarehouse_LargeCapacity()
    {
        var createRequest =
            new CreateWarehouseDto
            {
                Name = "Mega Warehouse",
                AddressLine1 = "123 St",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 1000000,
                StorageType = StorageType.DryStorage
            };

        await _warehouseService.CreateWarehouseAsync(createRequest);

        var warehouses =
            await _warehouseRepository.GetAllAsync();

        var warehouse = warehouses.First();

        Assert.That(
            warehouse.Capacity,
            Is.EqualTo(1000000m));
    }

    [Test]
    public async Task UpdateWarehouse_AllFieldsChange()
    {
        var warehouse =
            new Warehouse
            {
                Id = 1,
                Name = "Original",
                AddressLine1 = "Original St",
                AddressLine2 = "Original Floor",
                City = "Delhi",
                State = "Delhi",
                PostalCode = "110001",
                Capacity = 5000,
                AvailableCapacity = 5000,
                
                IsActive = true
            };

        await _warehouseRepository.AddAsync(warehouse);

        var updateRequest =
            new UpdateWarehouseDto
            {
                Name = "Updated",
                AddressLine1 = "Updated St",
                AddressLine2 = "Updated Floor",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400001",
                Capacity = 8000,
                StorageType = StorageType.ColdStorage
            };

        await _warehouseService.UpdateWarehouseAsync(1, updateRequest);

        var updatedWarehouse =
            await _warehouseRepository.GetByIdAsync(1);

        Assert.That(updatedWarehouse!.Name, Is.EqualTo("Updated"));
        Assert.That(updatedWarehouse.AddressLine1, Is.EqualTo("Updated St"));
        Assert.That(updatedWarehouse.AddressLine2, Is.EqualTo("Updated Floor"));
        Assert.That(updatedWarehouse.City, Is.EqualTo("Mumbai"));
        Assert.That(updatedWarehouse.State, Is.EqualTo("Maharashtra"));
        Assert.That(updatedWarehouse.PostalCode, Is.EqualTo("400001"));
        Assert.That(updatedWarehouse.Capacity, Is.EqualTo(8000m));
        Assert.That(updatedWarehouse.StorageType, Is.EqualTo(StorageType.ColdStorage));
    }

    #endregion

    #region Authorization Tests

    [Test]
    public async Task GetWarehouseById_OtherOwner_ThrowsForbidden()
    {
        var warehouse = new Warehouse
        {
            Id = 2,
            Name = "Other Warehouse",
            Capacity = 5000,
            AvailableCapacity = 5000,
            IsActive = true
        };
        await _warehouseRepository.AddAsync(warehouse);

        _mockCurrentUserService.Setup(m => m.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(m => m.AssignedWarehouseId).Returns(1);

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _warehouseService.GetWarehouseByIdAsync(2));
    }

    [Test]
    public async Task UpdateWarehouse_OtherOwner_ThrowsForbidden()
    {
        var warehouse = new Warehouse
        {
            Id = 2,
            Name = "Other Warehouse",
            Capacity = 5000,
            AvailableCapacity = 5000,
            IsActive = true
        };
        await _warehouseRepository.AddAsync(warehouse);

        _mockCurrentUserService.Setup(m => m.Role).Returns("WarehouseManager");
        _mockCurrentUserService.Setup(m => m.AssignedWarehouseId).Returns(1);

        var updateRequest = new UpdateWarehouseDto
        {
            Name = "Attempted Update",
            Capacity = 5000
        };

        Assert.ThrowsAsync<ForbiddenException>(
            async () => await _warehouseService.UpdateWarehouseAsync(2, updateRequest));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
