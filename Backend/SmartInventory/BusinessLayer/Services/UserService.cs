using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly ITokenService _tokenService;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IRepository<Role> roleRepository, ITokenService tokenService, IRepository<Warehouse> warehouseRepository, IEmailService emailService, IRepository<Supplier> supplierRepository, ApplicationDbContext context, ILogger<UserService> logger, IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _tokenService = tokenService;
            _warehouseRepository = warehouseRepository;
            _emailService = emailService;
            _supplierRepository = supplierRepository;
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task CreateUserAsync(
            CreateUserDto request)
        {
            _logger.LogInformation("Creating new user with email {Email}", request.Email);

            var existingUser =
                await _userRepository
                    .GetUserByEmailAsync(request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("Failed to create user: User with email {Email} already exists", request.Email);
                throw new ConflictException(
                    "User already exists.");
            }

            var role = await _roleRepository
                    .GetByIdAsync(request.RoleId);

            if (role == null)
            {
                throw new NotFoundException(
                    "Role not found.");
            }


            if (role.Name == "WarehouseManager" ||
                role.Name == "InventoryStaff")
            {
                if (!request.AssignedWarehouseId.HasValue)
                {
                    throw new BadRequestException(
                        "Warehouse assignment is required.");
                }
            }
            else
            {
                if (request.AssignedWarehouseId.HasValue)
                {
                    throw new BadRequestException(
                        "Only Warehouse Managers and Inventory Staff can be assigned to a warehouse.");
                }
            }

            if (request.AssignedWarehouseId.HasValue)
            {
                var warehouse =
                    await _warehouseRepository
                        .GetByIdAsync(
                            request.AssignedWarehouseId.Value);

                if (warehouse == null)
                {
                    throw new NotFoundException(
                        "Warehouse not found.");
                }
            }

            if(role.Name == "Supplier")
            {
                if(string.IsNullOrWhiteSpace(
                        request.SupplierCompanyName)
                ||
                string.IsNullOrWhiteSpace(
                        request.SupplierEmail))
                {
                    throw new BadRequestException(
                        "Supplier details are required.");
                }
            }


            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var user =
                    _mapper.Map<User>(request);

                user.IsPasswordSet = false;
                user.PasswordHash = string.Empty;

                if (role.Name == "Supplier")
                {
                    var supplier =
                        new Supplier
                        {
                            Name =
                                request.SupplierCompanyName!,
                            Email =
                                request.SupplierEmail!,
                            PhoneNumber =
                                request.SupplierPhone??"",
                            Address =
                                request.SupplierAddress??"",
                            IsActive = false
                        };

                    await _supplierRepository
                        .AddAsync(supplier);

                    user.SupplierId =
                        supplier.Id;
                }

                await _userRepository
                    .AddAsync(user);

                await transaction
                    .CommitAsync();

                _logger.LogInformation("Successfully created user {Email} with ID {UserId}", user.Email, user.Id);

                var token =
                    _tokenService
                        .GeneratePasswordSetupToken(
                            user.Id);

                var setupLink =
                    $"http://localhost:4200/set-password?token={token}";

                try
                {
                    await _emailService
                        .SendPasswordSetupEmailAsync(
                            user.Email,
                            user.Name,
                            role.Name,
                            setupLink);
                }
                catch (Exception ex)
                {
                    throw new EmailException(
                        "User created but failed to send email. Please contact support.",
                        ex);
                }
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                throw;
            }
        }


        public async Task<PagedResponseDto<UserResponseDto>>GetUsersAsync(PaginationParams pagination)
        {
            var users = await _userRepository.GetAllUsersWithRoleAsync();

            var totalRecords = users.Count();

            var pagedUsers = users.Skip((pagination.PageNumber - 1)* 
                    pagination.PageSize).Take(pagination.PageSize);

            return new PagedResponseDto<
                UserResponseDto>
            {
                Data = _mapper.Map<
                    IEnumerable<UserResponseDto>>(
                    pagedUsers),

                PageNumber =
                    pagination.PageNumber,

                PageSize =
                    pagination.PageSize,

                TotalRecords =
                    totalRecords,

                TotalPages =
                    (int)Math.Ceiling(
                        totalRecords /
                        (double)pagination.PageSize)
            };
        }

        public async Task<UserResponseDto?>
            GetUserByIdAsync(int id)
        {
            var user = await _userRepository
                .GetUserWithRoleAsync(id);

            if (user == null)
            {
                return null;
            }
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task ResendInviteAsync(
            int userId)
        {
            var user = await _userRepository
                .GetUserWithRoleAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(
                    "User not found.");
            }

            if (user.IsPasswordSet)
            {
                throw new BadRequestException(
                    "User has already activated account.");
            }

            var token = _tokenService
                .GeneratePasswordSetupToken(
                    user.Id);

            var setupLink =
                $"https://localhost:4200/set-password?token={token}";

            try
                {
                    await _emailService
                        .SendPasswordSetupEmailAsync(
                            user.Email,
                            user.Name,
                            user.Role.Name,
                            setupLink);
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Failed to send email. Please try again.");
                }
        }

        public async Task<IEnumerable<UserResponseDto>>
            GetInactiveUsersAsync()
        {
            var users = await _userRepository.FindAsync(u => !u.IsPasswordSet);

            return users.Select(_mapper.Map<UserResponseDto>);
        }


        public async Task<PagedResponseDto<UserResponseDto>>
            GetUsersAsync(
                PaginationParams pagination,
                string? role = null)
        {
            var users =
                await _userRepository
                    .GetAllUsersWithRoleAsync();

            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(
                    u => u.Role.Name.Equals(
                        role,
                        StringComparison.OrdinalIgnoreCase));
            }

            var totalRecords = users.Count();

            var pagedUsers = users
                .Skip(
                    (pagination.PageNumber - 1)
                    * pagination.PageSize)
                .Take(
                    pagination.PageSize);

            return new PagedResponseDto<UserResponseDto>
            {
                Data = _mapper.Map<
                    IEnumerable<UserResponseDto>>(
                    pagedUsers),

                PageNumber = pagination.PageNumber,

                PageSize = pagination.PageSize,

                TotalRecords = totalRecords,

                TotalPages = (int)Math.Ceiling(
                    totalRecords /
                    (double)pagination.PageSize)
            };
        }
}

}