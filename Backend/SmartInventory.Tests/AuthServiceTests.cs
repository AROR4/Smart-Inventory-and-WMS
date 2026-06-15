using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using Microsoft.Extensions.Configuration;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace SmartInventory.Tests;

[TestFixture]
public class AuthServiceTests
{
    private ApplicationDbContext _context = null!;

    private IUserRepository _userRepository = null!;

    private IRepository<Supplier> _supplierRepository = null!;

    private IRepository<Role> _roleRepository = null!;

    private ITokenService _tokenService = null!;

    private AuthService _authService = null!;

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

        _userRepository =
            new UserRepository(_context);

        _supplierRepository =
            new Repository<Supplier>(_context);

        _roleRepository =
            new Repository<Role>(_context);
        

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

        _authService =
            new AuthService(
                _userRepository,
                _supplierRepository,
                _tokenService,
                new Moq.Mock<Microsoft.Extensions.Logging.ILogger<AuthService>>().Object);
    }

    #region Login Tests

    [Test]
    public void LoginUserNotFound()
    {
        Assert.ThrowsAsync<
          InvalidCredentialsException>(
            async () =>
                await _authService.LoginAsync(
                    new LoginRequestDto
                    {
                        Email = "test@test.com",
                        Password = "Password123"
                    }));
    }

    [Test]
    public async Task LoginSuccess()
    {
        var role =
            new Role
            {
                Id = 1,
                Name = "Admin"
            };

        _context.Roles.Add(role);

        var user =
            new User
            {
                Name = "Raghav",
                Email = "test@test.com",
                IsPasswordSet = true,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "Password123"),
                RoleId = role.Id,
                Role = role
            };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var result =
            await _authService.LoginAsync(
                new LoginRequestDto
                {
                    Email = "test@test.com",
                    Password = "Password123"
                });

        Assert.That(
            result.Token,
            Is.Not.Empty);

        Assert.That(
            result.Email,
            Is.EqualTo(
                "test@test.com"));
    }

    [Test]
    public async Task PasswordNotSet()
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
            Email = "test@test.com",
            IsPasswordSet = false,
            PasswordHash = string.Empty,
            RoleId = role.Id
        };

        await _userRepository.AddAsync(user);

        Assert.ThrowsAsync<
            BadRequestException>(
            async () =>
                await _authService.LoginAsync(
                    new LoginRequestDto
                    {
                        Email = "test@test.com",
                        Password = "Password123"
                    }));
    }

    [Test]
    public async Task InvalidPassword()
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
            Email = "test@test.com",
            IsPasswordSet = true,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "CorrectPassword"),

            RoleId = role.Id
        };

        await _userRepository.AddAsync(user);

        Assert.ThrowsAsync<
                InvalidCredentialsException>(
            async () =>
                await _authService.LoginAsync(
                    new LoginRequestDto
                    {
                        Email = "test@test.com",
                        Password = "WrongPassword"
                    }));
    }

    #endregion

    #region SetPassword Tests

    [Test]
    public void SetPasswordWithInvalidPurpose()
    {
        //creating a token for loggin purpose
        var token =
            _tokenService.GenerateToken(
                new TokenRequest
                {
                    Id = 1,
                    Name = "Test",
                    Email = "test@test.com",
                    Role = "Admin"
                });

        Assert.ThrowsAsync<
            BadRequestException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = token,
                        Password = "Password123"
                    }));
    }

    [Test]
    public void SetPasswordForInvalidUser()
    {
        var token =
            _tokenService
                .GeneratePasswordSetupToken(999);

        Assert.ThrowsAsync<
            BadRequestException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = token,
                        Password = "Password123"
                    }));
    }

    [Test]
    public async Task SetPasswordSuccess()
    {
        var user = new User
        {
            Id = 1,
            Name = "Raghav",
            Email = "test@test.com",
            IsPasswordSet = false
        };

        await _userRepository.AddAsync(user);
    
        var token =
            _tokenService
                .GeneratePasswordSetupToken(user.Id);

        await _authService.SetPasswordAsync(
            new SetPasswordDto
            {
                Token = token,
                Password = "Password123"
            });

        var updatedUser =await _userRepository.GetByIdAsync(user.Id);

        Assert.That(
            updatedUser!.IsPasswordSet,
            Is.True);

        Assert.That(
            BCrypt.Net.BCrypt.Verify(
                "Password123",
                updatedUser.PasswordHash),
            Is.True);
    }

    [Test]
    public async Task SetPasswordForSupplier()
    {
        var supplier = new Supplier
        {
            Id = 1,
            Name = "ABC Traders",
            Email = "abc@test.com",
            IsActive = false
        };

        await _supplierRepository.AddAsync(supplier);

        var user = new User
        {
            Id = 1,
            Name = "Supplier User",
            Email = "supplier@test.com",
            IsPasswordSet = false,
            SupplierId = supplier.Id
        };

        await _userRepository.AddAsync(user);

        var token =
            _tokenService
                .GeneratePasswordSetupToken(user.Id);

        await _authService.SetPasswordAsync(
            new SetPasswordDto
            {
                Token = token,
                Password = "Password123"
            });

        var updatedSupplier =
            await _supplierRepository.GetByIdAsync(supplier.Id);

        Assert.That(
            updatedSupplier!.IsActive,
            Is.True);
    }

    [Test]
    public async Task SetPasswordForSupplierNotFound()
    {
        var user = new User
        {
            Id = 1,
            Name = "Supplier User",
            Email = "supplier@test.com",
            IsPasswordSet = false,
            SupplierId = 999
        };

        await _userRepository.AddAsync(user);

        var token =
            _tokenService
                .GeneratePasswordSetupToken(user.Id);

        Assert.ThrowsAsync<
            NotFoundException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = token,
                        Password = "Password123"
                    }));
    }

    [Test]
    public async Task SetPasswordForPasswordAlreadySet()
    {
        var user = new User
        {
            Id = 1,
            Name = "Raghav",
            Email = "test@test.com",
            IsPasswordSet = true
        };

        await _userRepository.AddAsync(user);

        var token =_tokenService.GeneratePasswordSetupToken(user.Id);

        Assert.ThrowsAsync<
            BadRequestException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = token,
                        Password = "Password123"
                    }));
    }

    [Test]
    public void SetPasswordAsync_ExpiredToken()
    {
        var expiredToken =
            GenerateExpiredPasswordToken(1);

        Assert.ThrowsAsync<
            SecurityTokenExpiredException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = expiredToken,
                        Password = "Password123"
                    }));
    }

    [Test]
    public async Task SetPasswordSupplierAlreadyActive()
    {
        var supplier = new Supplier
        {
            Id = 1,
            IsActive = true,
            Name = "ABC",
            Email = "abc@test.com"
        };

        await _supplierRepository.AddAsync(supplier);

        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Name = "Test",
            SupplierId = supplier.Id,
            IsPasswordSet = false
        };

        await _userRepository.AddAsync(user);

        var token =
            _tokenService.GeneratePasswordSetupToken(user.Id);

        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _authService.SetPasswordAsync(
                    new SetPasswordDto
                    {
                        Token = token,
                        Password = "Password123"
                    }));
    }
    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }


    private string GenerateExpiredPasswordToken(
        int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                "Purpose",
                "PasswordSetup")
        };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "ThisIsMySuperSecretKeyForTesting123456"));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: "SmartInventoryManagement",
                audience: "SmartInventoryUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(-5),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}