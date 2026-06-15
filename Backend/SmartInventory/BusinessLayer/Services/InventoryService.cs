using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILowStockAlertService _lowStockAlertService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryService> _logger;
        private readonly IMapper _mapper;

        public InventoryService(
            IInventoryRepository inventoryRepository,
            IProductRepository productRepository,
            IRepository<Warehouse> warehouseRepository,
            IStockMovementRepository stockMovementRepository,
            ICurrentUserService currentUserService,
            ILowStockAlertService lowStockAlertService,
            ApplicationDbContext context,
            ILogger<InventoryService> logger,
            IMapper mapper)
        {
            _inventoryRepository = inventoryRepository;
            _productRepository = productRepository;
            _warehouseRepository = warehouseRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUserService = currentUserService;
            _lowStockAlertService = lowStockAlertService;
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

         public async Task AddStockAsync(
            AddStockDto request)
        {
            _logger.LogInformation(
                "Adding {Quantity} units of product {ProductId} to warehouse {WarehouseId}",
                request.Quantity,
                request.ProductId,
                request.WarehouseId);

            var isOuterTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = isOuterTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var product =await _productRepository.GetByIdAsync(request.ProductId);

                if (product == null || !product.IsActive)
                {
                    throw new NotFoundException("Product not found.");
                }

                var warehouse =await _warehouseRepository.GetByIdAsync(request.WarehouseId);

                if (warehouse == null || !warehouse.IsActive)
                {
                    throw new NotFoundException("Warehouse not found.");
                }

                if (product.RequiredStorageType
                    != warehouse.StorageType)
                {
                    throw new BadRequestException(
                        "Product cannot be stored in this warehouse.");
                }

                var volumePerUnit = product.Length *
                                    product.Width *
                                    product.Height;

                var requiredVolume =volumePerUnit *
                                    request.Quantity;

                var effectiveCapacity =
                    warehouse.AvailableCapacity -
                    warehouse.ReservedCapacity;

                if (requiredVolume > effectiveCapacity)
                {
                    _logger.LogWarning(
                        "Warehouse {WarehouseId} capacity exceeded for adding stock. Required Volume: {RequiredVolume}, Effective Capacity: {EffectiveCapacity}",
                        request.WarehouseId,
                        requiredVolume,
                        effectiveCapacity);
                    throw new BadRequestException("Insufficient warehouse capacity.");
                }

                var inventory =await _inventoryRepository.GetInventoryAsync(request.WarehouseId,request.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = request.ProductId,
                        WarehouseId = request.WarehouseId,
                        Quantity = request.Quantity,
                        LastUpdated = DateTime.Now
                    };

                    await _inventoryRepository.AddAsync(inventory);
                }
                else
                {
                    inventory.Quantity +=request.Quantity;

                    inventory.LastUpdated =DateTime.Now;

                    await _inventoryRepository.UpdateAsync(inventory);
                }

                warehouse.AvailableCapacity -=requiredVolume;

                await _warehouseRepository.UpdateAsync(warehouse);

                await _lowStockAlertService.CheckAndUpdateAlertAsync(request.ProductId,request.WarehouseId);

                await _stockMovementRepository
                .AddAsync(
                    new StockMovement
                    {
                        ProductId =
                            request.ProductId,

                        WarehouseId =
                            request.WarehouseId,

                        Quantity =
                            request.Quantity,

                        Type =
                            StockMovementType.StockIn,

                        Reason =
                                string.IsNullOrWhiteSpace(
                                request.Reason)
                                ? "Stock Added"
                                : request.Reason,

                        CreatedAt =
                            DateTime.Now,

                        CreatedByUserId =
                            _currentUserService.UserId
                    });

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation(
                    "Successfully added {Quantity} units of product {ProductId} to warehouse {WarehouseId}. New quantity: {NewQuantity}",
                    request.Quantity,
                    request.ProductId,
                    request.WarehouseId,
                    inventory.Quantity);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }

        public async Task RemoveStockAsync(
            RemoveStockDto request)
        {
            _logger.LogInformation(
                "Removing {Quantity} units of product {ProductId} from warehouse {WarehouseId}",
                request.Quantity,
                request.ProductId,
                request.WarehouseId);

            var isOuterTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = isOuterTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var inventory =
                    await _inventoryRepository
                        .GetInventoryAsync(
                            request.WarehouseId,
                            request.ProductId);

                if (inventory == null)
                {
                    throw new NotFoundException(
                        "Inventory record not found.");
                }

                if (inventory.Quantity < request.Quantity)
                {
                    _logger.LogWarning(
                        "Failed to remove stock: Insufficient stock of product {ProductId} in warehouse {WarehouseId}. Available: {AvailableQuantity}, Requested: {RequestedQuantity}",
                        request.ProductId,
                        request.WarehouseId,
                        inventory.Quantity,
                        request.Quantity);
                    throw new BadRequestException(
                        "Insufficient stock available.");
                }

                var product =
                    await _productRepository
                        .GetByIdAsync(request.ProductId);

                var warehouse =
                    await _warehouseRepository
                        .GetByIdAsync(request.WarehouseId);

                var releasedVolume =
                    product!.Length *
                    product.Width *
                    product.Height *
                    request.Quantity;

                inventory.Quantity -=
                    request.Quantity;

                inventory.LastUpdated =
                    DateTime.Now;

                warehouse!.AvailableCapacity +=
                    releasedVolume;

                await _inventoryRepository
                    .UpdateAsync(inventory);

                await _warehouseRepository
                    .UpdateAsync(warehouse);

                await _lowStockAlertService.CheckAndUpdateAlertAsync(request.ProductId,request.WarehouseId);

                await _stockMovementRepository
                .AddAsync(
                    new StockMovement
                    {
                        ProductId =
                            request.ProductId,

                        WarehouseId =
                            request.WarehouseId,

                        Quantity =
                            request.Quantity,

                        Type =
                            StockMovementType.StockOut,

                        Reason =
                            string.IsNullOrWhiteSpace(
                                request.Reason)
                                ? "Stock Removed"
                                : request.Reason,

                        CreatedAt =
                            DateTime.Now,

                        CreatedByUserId =
                            _currentUserService.UserId
                    });
            
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation(
                    "Successfully removed {Quantity} units of product {ProductId} from warehouse {WarehouseId}. New quantity: {NewQuantity}",
                    request.Quantity,
                    request.ProductId,
                    request.WarehouseId,
                    inventory.Quantity);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }

        public async Task AdjustStockAsync(
            AdjustStockDto request)
        {
            _logger.LogInformation(
                "Adjusting stock for product {ProductId} in warehouse {WarehouseId} to new quantity {NewQuantity}",
                request.ProductId,
                request.WarehouseId,
                request.NewQuantity);

            var isOuterTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = isOuterTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var inventory =
                    await _inventoryRepository
                        .GetInventoryAsync(
                            request.WarehouseId,
                            request.ProductId);
                
                if (inventory == null)
                {
                    throw new NotFoundException(
                        "Inventory record not found.");
                }

                if(_currentUserService.Role =="WarehouseManager" && 
                request.WarehouseId != _currentUserService.AssignedWarehouseId)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted unauthorized stock adjustment for product {ProductId} in warehouse {WarehouseId}",
                        _currentUserService.UserId,
                        request.ProductId,
                        request.WarehouseId);
                    throw new ForbiddenException(
                        "You are not authorized to perform this action.");
                }

                var product =
                    await _productRepository
                        .GetByIdAsync(request.ProductId);

                var warehouse =
                    await _warehouseRepository
                        .GetByIdAsync(request.WarehouseId);

                var volumePerUnit =
                    product!.Length *
                    product.Width *
                    product.Height;

                var oldVolume =
                    volumePerUnit *
                    inventory.Quantity;

                var newVolume =
                    volumePerUnit *
                    request.NewQuantity;

                var difference =
                    newVolume - oldVolume;

                var effectiveCapacity =
                    warehouse!.AvailableCapacity -
                    warehouse.ReservedCapacity;

                if (difference > 0 &&
                    difference > effectiveCapacity)
                {
                    _logger.LogWarning(
                        "Failed to adjust stock: Insufficient warehouse capacity in warehouse {WarehouseId}. Required Difference: {Difference}, Effective Capacity: {EffectiveCapacity}",
                        request.WarehouseId,
                        difference,
                        effectiveCapacity);
                    throw new BadRequestException(
                        "Insufficient warehouse capacity.");
                }

                warehouse!.AvailableCapacity -=
                    difference;

                var oldQuantity =
                    inventory.Quantity;

                inventory.Quantity =
                    request.NewQuantity;

                inventory.LastUpdated =
                    DateTime.Now;

                var adjustmentQuantity =
                    request.NewQuantity - oldQuantity;

                await _inventoryRepository
                    .UpdateAsync(inventory);

                await _warehouseRepository
                    .UpdateAsync(warehouse);

                await _lowStockAlertService
                    .CheckAndUpdateAlertAsync(
                        request.ProductId,
                        request.WarehouseId);

                await _stockMovementRepository
                .AddAsync(
                    new StockMovement
                    {
                        ProductId =
                            request.ProductId,

                        WarehouseId =
                            request.WarehouseId,

                        Quantity =
                            adjustmentQuantity,

                        Type =
                            StockMovementType.Adjustment,

                        Reason =
                            string.IsNullOrWhiteSpace(
                                request.Reason)
                                ? "Stock Adjusted"
                                : request.Reason,

                        CreatedAt =
                            DateTime.Now,

                        CreatedByUserId =_currentUserService.UserId
                    });

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation(
                    "Successfully adjusted stock for product {ProductId} in warehouse {WarehouseId}. Old quantity: {OldQuantity}, new quantity: {NewQuantity}",
                    request.ProductId,
                    request.WarehouseId,
                    oldQuantity,
                    request.NewQuantity);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }

        public async Task<
            PagedResponseDto<InventoryResponseDto>>
            GetInventoryAsync(
                PaginationParams pagination,
                InventoryFilterDto filter)
        {
            if (_currentUserService.Role == "WarehouseManager")
            {
                if (filter.WarehouseId.HasValue && filter.WarehouseId.Value != _currentUserService.AssignedWarehouseId)
                {
                    throw new ForbiddenException("You are not authorized to access this warehouse's inventory.");
                }
                filter.WarehouseId =
                    _currentUserService.AssignedWarehouseId;
            }

            var inventories =
                await _inventoryRepository
                    .GetInventoryWithDetailsAsync();

            if (filter.ProductId.HasValue)
            {
                inventories = inventories.Where(
                    i => i.ProductId ==
                        filter.ProductId.Value);
            }

            if (filter.WarehouseId.HasValue)
            {
                inventories = inventories.Where(
                    i => i.WarehouseId ==
                        filter.WarehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                inventories = inventories.Where(i =>
                    i.Product.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    i.Product.SKU.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    i.Product.Barcode.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    i.Warehouse.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    i.Product.Company.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase));
            }

            var totalRecords =
                inventories.Count();

            var pagedInventories =
                inventories
                    .Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                InventoryResponseDto>
            {
                Data = _mapper.Map<
                    IEnumerable<
                        InventoryResponseDto>>(
                    pagedInventories),

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

        public async Task ExecuteStoreInventoryAsync(
            int productId,
            int warehouseId,
            int quantity,
            string reason)
        {
            await AddStockAsync(
                new AddStockDto
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = quantity,
                    Reason = reason
                });
        }

        public async Task ExecuteRetrieveInventoryAsync(
            int productId,
            int warehouseId,
            int quantity,
            string reason)
        {
            await RemoveStockAsync(
                new RemoveStockDto
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = quantity,
                    Reason = reason
                });
        }

    }
}
