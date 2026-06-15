using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.EntityFrameworkCore.Diagnostics;


[TestFixture]
public class UserServiceTests
{
    private ApplicationDbContext _context = null!;

    private IUserRepository _userRepository = null!;

    private IRepository<Role> _roleRepository = null!;

    private IRepository<Warehouse> _warehouseRepository = null!;

    private IRepository<Supplier> _supplierRepository = null!;

    private Mock<IEmailService> _emailService = null!;

    private ITokenService _tokenService = null!;

    private IMapper _mapper = null!;

    private UserService _userService = null!;

    [SetUp]
    public void Setup()
    {
      
    var options =
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .ConfigureWarnings(x =>
                x.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context =
            new ApplicationDbContext(options);

        _userRepository =
            new UserRepository(_context);

        _roleRepository =
            new Repository<Role>(_context);

        _warehouseRepository =
            new Repository<Warehouse>(_context);

        _supplierRepository =
            new Repository<Supplier>(_context);

        _emailService =
            new Mock<IEmailService>();

        var settings =
            new Dictionary<string, string?>
            {
                {"JWT:Key","ThisIsMySuperSecretKeyForTesting123456"},
                {"JWT:Issuer","SmartInventoryManagement"},
                {"JWT:Audience","SmartInventoryUsers"},
                {"JWT:DurationInMinutes","60"}
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

        _tokenService =
            new TokenService(configuration);

        var mapperConfig =
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

        _mapper =
            mapperConfig.CreateMapper();

        _userService =
            new UserService(
                _userRepository,
                _roleRepository,
                _tokenService,
                _warehouseRepository,
                _emailService.Object,
                _supplierRepository,
                _context,
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<UserService>>().Object,
                _mapper);
    }

    #region CreateUser Tests

    [Test]
    public async Task CreateUser_UserAlreadyExists()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);
        
        var existingUser = new User
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            IsPasswordSet = true,
            RoleId = role.Id,
            Role= role,
        };

        await _userRepository.AddAsync(existingUser);

        var request = new CreateUserDto
        {
            Name = "New User",
            Email = "raghav@test.com",
            RoleId = 1
        };

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    [Test]
    public void CreateUser_RoleNotFound()
    {
        var request = new CreateUserDto
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            RoleId = 999
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    [Test]
    public async Task CreateUser_WarehouseManagerWithoutWarehouse()
    {
        var role = new Role
        {
            Id = 1,
            Name = "WarehouseManager"
        };

        await _roleRepository.AddAsync(role);

        var request = new CreateUserDto
        {
            Name = "Manager",
            Email = "manager@test.com",
            RoleId = role.Id
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    [Test]
    public async Task CreateUser_AdminWithWarehouseAssigned()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        var request = new CreateUserDto
        {
            Name = "Admin User",
            Email = "admin@test.com",
            RoleId = role.Id,
            AssignedWarehouseId = 1
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    [Test]
    public async Task CreateUser_WarehouseNotFound()
    {
        var role = new Role
        {
            Id = 1,
            Name = "WarehouseManager"
        };

        await _roleRepository.AddAsync(role);

        var request = new CreateUserDto
        {
            Name = "Manager",
            Email = "manager@test.com",
            RoleId = role.Id,
            AssignedWarehouseId = 999
        };

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    [Test]
    public async Task CreateUser_SupplierMissingDetails()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Supplier"
        };

        await _roleRepository.AddAsync(role);

        var request = new CreateUserDto
        {
            Name = "Supplier User",
            Email = "supplier@test.com",
            RoleId = role.Id
        };

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }


    [Test]
    public async Task CreateUser_Success()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        _emailService
            .Setup(x =>
                x.SendPasswordSetupEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var request = new CreateUserDto
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            RoleId = role.Id
        };

        await _userService.CreateUserAsync(request);

        var user =
            await _userRepository
                .GetUserByEmailAsync(
                    request.Email);

        Assert.That(user, Is.Not.Null);

        Assert.That(
            user!.IsPasswordSet,
            Is.False);
    }


    [Test]
    public async Task CreateUser_SupplierSuccess()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Supplier"
        };

        await _roleRepository.AddAsync(role);

        _emailService
            .Setup(x =>
                x.SendPasswordSetupEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var request = new CreateUserDto
        {
            Name = "Supplier User",
            Email = "supplier@test.com",
            RoleId = role.Id,

            SupplierCompanyName = "ABC Traders",
            SupplierEmail = "abc@test.com"
        };

        await _userService.CreateUserAsync(request);

        var user =
            await _userRepository
                .GetUserByEmailAsync(
                    request.Email);

        Assert.That(
            user!.SupplierId,
            Is.Not.Null);

        var supplier =
            await _supplierRepository
                .GetByIdAsync(
                    user.SupplierId!.Value);

        Assert.That(
            supplier!.IsActive,
            Is.False);
    }


    [Test]
    public void CreateUser_EmailFailure()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        _roleRepository
            .AddAsync(role)
            .Wait();

        _emailService
            .Setup(x =>
                x.SendPasswordSetupEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .ThrowsAsync(
                new Exception("SMTP Error"));

        var request = new CreateUserDto
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            RoleId = role.Id
        };

        Assert.ThrowsAsync<EmailException>(
            async () =>
                await _userService.CreateUserAsync(request));
    }

    #endregion

    #region ResendInvite Tests

    [Test]
    public void ResendInvite_UserNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _userService.ResendInviteAsync(
                    999));
    }

    [Test]
    public async Task ResendInvite_UserAlreadyActivated()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        var user = new User
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            IsPasswordSet = true,
            RoleId = role.Id
        };

        await _userRepository.AddAsync(user);

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _userService.ResendInviteAsync(
                    user.Id));
    }

    [Test]
    public async Task ResendInvite_EmailFailure()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        var user = new User
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            IsPasswordSet = false,
            RoleId = role.Id,
            Role = role
        };

        await _userRepository.AddAsync(user);

        _emailService
            .Setup(x =>
                x.SendPasswordSetupEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .ThrowsAsync(
                new Exception("SMTP Failed"));

        Assert.ThrowsAsync<BadRequestException>(
            async () =>
                await _userService.ResendInviteAsync(
                    user.Id));
    }


    [Test]
    public async Task ResendInvite_Success()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        var user = new User
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            IsPasswordSet = false,
            RoleId = role.Id,
            Role = role
        };

        await _userRepository.AddAsync(user);

        _emailService
            .Setup(x =>
                x.SendPasswordSetupEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _userService.ResendInviteAsync(
            user.Id);

        _emailService.Verify(
            x => x.SendPasswordSetupEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }


    #endregion

    #region GetUserById Tests

    [Test]
    public async Task GetUserById_NotFound()
    {
        var result =
            await _userService
                .GetUserByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetUserById_Success()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        var user = new User
        {
            Name = "Raghav",
            Email = "raghav@test.com",
            RoleId = role.Id,
            Role = role
        };

        await _userRepository.AddAsync(user);

        var result =
            await _userService
                .GetUserByIdAsync(
                    user.Id);

        Assert.That(
            result,
            Is.Not.Null);

        Assert.That(
            result!.Email,
            Is.EqualTo(
                user.Email));
    }


    #endregion

    #region GetInactiveUsers Tests

    [Test]
    public async Task GetInactiveUsers_ReturnsOnlyInactive()
    {
        await _userRepository.AddAsync(
            new User
            {
                Name = "Inactive",
                Email = "inactive@test.com",
                IsPasswordSet = false
            });

        await _userRepository.AddAsync(
            new User
            {
                Name = "Active",
                Email = "active@test.com",
                IsPasswordSet = true
            });

        var result =
            await _userService
                .GetInactiveUsersAsync();

        Assert.That(
            result.Count(),
            Is.EqualTo(1));

        Assert.That(
            result.First().Email,
            Is.EqualTo(
                "inactive@test.com"));
    }


    #endregion

    #region GetUsers Tests

    [Test]
    public async Task GetUsers_ReturnsPagedData()
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        await _roleRepository.AddAsync(role);

        for (int i = 1; i <= 3; i++)
        {
            await _userRepository.AddAsync(
                new User
                {
                    Name = $"User{i}",
                    Email = $"user{i}@test.com",
                    RoleId = role.Id,
                    Role = role
                });
        }

        var result =
            await _userService
                .GetUsersAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 2
                    });

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(2));

        Assert.That(
            result.TotalRecords,
            Is.EqualTo(3));
    }


    [Test]
    public async Task GetUsers_FilterByRole()
    {
        var adminRole = new Role
        {
            Id = 1,
            Name = "Admin"
        };

        var supplierRole = new Role
        {
            Id = 2,
            Name = "Supplier"
        };

        await _roleRepository.AddAsync(adminRole);
        await _roleRepository.AddAsync(supplierRole);

        await _userRepository.AddAsync(
            new User
            {
                Name = "Admin1",
                Email = "admin@test.com",
                RoleId = adminRole.Id,
                Role = adminRole
            });

        await _userRepository.AddAsync(
            new User
            {
                Name = "Supplier1",
                Email = "supplier@test.com",
                RoleId = supplierRole.Id,
                Role = supplierRole
            });

        var result =
            await _userService
                .GetUsersAsync(
                    new PaginationParams
                    {
                        PageNumber = 1,
                        PageSize = 10
                    },
                    "Admin");

        Assert.That(
            result.Data.Count(),
            Is.EqualTo(1));

        Assert.That(
            result.Data.First().Role,
            Is.EqualTo("Admin"));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}