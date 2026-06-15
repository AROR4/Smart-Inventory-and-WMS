using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class LowStockAlertService
        : ILowStockAlertService
    {
        private readonly
            ILowStockAlertRepository
            _lowStockAlertRepository;

        private readonly
            IInventoryRepository
            _inventoryRepository;

        private readonly
            ILogger<LowStockAlertService>
            _logger;

        private readonly
            IMapper
            _mapper;

        public LowStockAlertService(
            ILowStockAlertRepository
                lowStockAlertRepository,
            IInventoryRepository
                inventoryRepository,
            ILogger<LowStockAlertService> logger,
            IMapper mapper)
        {
            _lowStockAlertRepository =
                lowStockAlertRepository;

            _inventoryRepository =
                inventoryRepository;

            _logger = logger;

            _mapper = mapper;
        }

        public async Task
            CheckAndUpdateAlertAsync(
                int productId,
                int warehouseId)
        {
            var inventory =
                await _inventoryRepository
                    .GetInventoryAsync(
                        warehouseId,
                        productId);

            if (inventory == null)
            {
                _logger.LogWarning("Cannot check or update low stock alert. Inventory not found for product ID {ProductId} in warehouse ID {WarehouseId}.", productId, warehouseId);
                return;
            }

            var existingAlert =
                await _lowStockAlertRepository
                    .GetActiveAlertAsync(
                        productId,
                        warehouseId);

            var isLowStock =
                inventory.Quantity <=
                inventory.Product.ReorderLevel;

            if (isLowStock)
            {
                if (existingAlert == null)
                {
                    await _lowStockAlertRepository
                        .AddAsync(
                            new LowStockAlert
                            {
                                ProductId =
                                    productId,

                                WarehouseId =
                                    warehouseId,

                                CurrentQuantity =
                                    inventory.Quantity,

                                ReorderLevel =
                                    inventory.Product
                                        .ReorderLevel,

                                IsResolved =
                                    false
                            });
                    _logger.LogInformation("Low stock alert created for product ID {ProductId} in warehouse ID {WarehouseId}. Current quantity: {Quantity}, Reorder level: {ReorderLevel}.", productId, warehouseId, inventory.Quantity, inventory.Product.ReorderLevel);
                }
                else
                {
                    existingAlert.CurrentQuantity =
                        inventory.Quantity;

                    existingAlert.ReorderLevel =
                        inventory.Product
                            .ReorderLevel;

                    await _lowStockAlertRepository
                        .UpdateAsync(
                            existingAlert);
                    _logger.LogInformation("Low stock alert updated for product ID {ProductId} in warehouse ID {WarehouseId}. Current quantity: {Quantity}, Reorder level: {ReorderLevel}.", productId, warehouseId, inventory.Quantity, inventory.Product.ReorderLevel);
                }
            }
            else
            {
                if (existingAlert != null)
                {
                    existingAlert.IsResolved =
                        true;

                    await _lowStockAlertRepository
                        .UpdateAsync(
                            existingAlert);
                    _logger.LogInformation("Low stock alert resolved for product ID {ProductId} in warehouse ID {WarehouseId}. Current quantity: {Quantity}, Reorder level: {ReorderLevel}.", productId, warehouseId, inventory.Quantity, inventory.Product.ReorderLevel);
                }
            }
        }

        public async Task<
            PagedResponseDto<
                LowStockAlertResponseDto>>
            GetActiveAlertsAsync(
                PaginationParams pagination)
        {
            _logger.LogInformation("Fetching active low stock alerts for page {PageNumber} with page size {PageSize}.", pagination.PageNumber, pagination.PageSize);
            var alerts =
                await _lowStockAlertRepository
                    .GetActiveAlertsAsync();

            var totalRecords =
                alerts.Count();

            var pagedAlerts =
                alerts
                    .Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                LowStockAlertResponseDto>
            {
                Data =
                    _mapper.Map<
                        IEnumerable<
                            LowStockAlertResponseDto>>(
                                pagedAlerts),

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
    }
}