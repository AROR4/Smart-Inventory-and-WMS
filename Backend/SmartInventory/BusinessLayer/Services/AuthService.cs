using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCrypt.Net;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IRepository<Supplier> supplierRepository, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _supplierRepository = supplierRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            _logger.LogInformation("Attempting login for email {Email}", request.Email);

            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt: user with email {Email} not found", request.Email);
                throw new InvalidCredentialsException("Invalid email or password.");
            }

            if (!user.IsPasswordSet)
            {
                _logger.LogWarning("Failed login attempt: password is not set for email {Email}", request.Email);
                throw new BadRequestException(
                    "Please set your password first.");
            }

            var isPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Failed login attempt: invalid password for email {Email}", request.Email);
                throw new InvalidCredentialsException("Invalid email or password.");
            }

            var tokenRequest = new TokenRequest
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.Name,
                AssignedWarehouseId = user.AssignedWarehouseId,
                SupplierId = user.SupplierId
            };

            var token =_tokenService.GenerateToken(tokenRequest);

            _logger.LogInformation("Successfully logged in user with email {Email}", request.Email);

            return new AuthResponseDto
            {
                Token = token,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.Name,
                AssignedWarehouseId = user.AssignedWarehouseId,
                SupplierId = user.SupplierId

            };
        }


        public async Task SetPasswordAsync(
            SetPasswordDto request)
        {
            _logger.LogInformation("Starting password setup");

            var principal =_tokenService.ValidateToken(request.Token);

            var purpose =principal.FindFirst("Purpose")?.Value;

            if (purpose != "PasswordSetup")
            {
                _logger.LogWarning("Password setup failed: invalid token purpose");
                throw new BadRequestException(
                    "Invalid token.");
            }
            
            var userId = int.Parse(
                principal.FindFirst(
                    ClaimTypes.NameIdentifier)!.Value);
            
            
            var user =
                await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Password setup failed: user with ID {UserId} not found", userId);
                throw new BadRequestException(
                    "Invalid token.");
            }

            if(user.IsPasswordSet)
            {
                _logger.LogWarning("Password setup failed: password is already set for user {UserId}", userId);
                throw new BadRequestException(
                    "Password is already set.");
            }

            user.PasswordHash =BCrypt.Net.BCrypt.HashPassword(request.Password);

            user.IsPasswordSet = true;

            if( user.SupplierId.HasValue)
            {
                var supplier =
                    await _supplierRepository
                        .GetByIdAsync(
                            user.SupplierId.Value);

                if(supplier != null &&
                !supplier.IsActive)
                {
                    supplier.IsActive = true;

                    await _supplierRepository
                        .UpdateAsync(supplier);
                }
                else
                {
                    throw new NotFoundException("Associated supplier not found.");
                }
            }
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password setup completed successfully for user {UserId}", userId);
        }
    }
}