using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class WarehouseTransferService : IWarehouseTransferService
    {
        private readonly IWarehouseTransferRepository _transferRepository;
        private readonly IProductService _productService;
        private readonly IWarehouseService _warehouseService;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILogger<WarehouseTransferService> _logger;
        private readonly IMapper _mapper;

        public WarehouseTransferService(IWarehouseTransferRepository transferRepository, IProductService productService, IWarehouseService warehouseService, IRepository<Warehouse> warehouseRepository, IProductRepository productRepository, ICurrentUserService currentUserService, IInventoryRepository inventoryRepository, ILogger<WarehouseTransferService> logger, IMapper mapper)
        {
            _transferRepository = transferRepository;
            _productService = productService;
            _warehouseService = warehouseService;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
            _mapper = mapper;
        }

       public async Task ApproveTransferAsync(
             int id)
        {
            _logger.LogInformation(
                "Approving warehouse transfer {TransferId}",
                id);

            var transfer =
                await _transferRepository
                    .GetByIdAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Warehouse transfer not found.");
            }

            if (transfer.Status !=
                TransferStatus.Pending)
            {
                throw new ConflictException(
                    "Only pending transfers can be approved.");
            }

            var destinationWarehouse =
                await _warehouseRepository
                    .GetByIdAsync(
                        transfer.DestinationWarehouseId);

            if (destinationWarehouse == null)
            {
                throw new NotFoundException(
                    "Destination warehouse not found.");
            }

            var effectiveCapacity =
                destinationWarehouse.AvailableCapacity
                - destinationWarehouse.ReservedCapacity;

            if (transfer.TransferVolume >
                effectiveCapacity)
            {
                _logger.LogWarning(
                    "Destination warehouse {WarehouseId} capacity exceeded for approved transfer {TransferId}",
                    transfer.DestinationWarehouseId,
                    id);
                throw new BadRequestException(
                    "Insufficient warehouse capacity.");
            }

            destinationWarehouse.ReservedCapacity +=
                transfer.TransferVolume;

            transfer.Status =
                TransferStatus.Approved;

            await _warehouseRepository
                .UpdateAsync(destinationWarehouse);

            await _transferRepository
                .UpdateAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferId} approved successfully",
                id);
        }

        // only helper method as it will automatically marked completed when staff place the recievd goods in the warehouse 
        public async Task CompleteTransferAsync(
            int id)
        {
            _logger.LogInformation(
                "Completing warehouse transfer {TransferId}",
                id);

            var transfer =
                await _transferRepository
                    .GetByIdAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Transfer not found.");
            }

            if (transfer.Status !=
                TransferStatus.Received)
            {
                throw new ConflictException(
                    "Only received transfers can be completed.");
            }
            var destinationWarehouse = await _warehouseRepository.GetByIdAsync(transfer.DestinationWarehouseId);

            destinationWarehouse!.ReservedCapacity -=
                transfer.TransferVolume;

            if (destinationWarehouse.ReservedCapacity < 0)
            {
                destinationWarehouse.ReservedCapacity = 0;
            }

            transfer.Status =
                TransferStatus.Completed;

            transfer.CompletedDate =
                DateTime.Now;

            await _warehouseRepository
                .UpdateAsync(destinationWarehouse);

            await _transferRepository
                .UpdateAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferId} completed successfully",
                id);
        }

        public async Task RejectTransferAsync(
            int id,
            string reason)
        {
            _logger.LogInformation(
                "Rejecting warehouse transfer {TransferId}",
                id);

            var transfer =
                await _transferRepository
                    .GetByIdAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Transfer not found.");
            }

            if (transfer.Status !=
                TransferStatus.Pending)
            {
                throw new ConflictException(
                    "Only pending transfers can be rejected.");
            }

            transfer.Status =
                TransferStatus.Rejected;

            transfer.RejectionReason =
                reason;

            await _transferRepository
                .UpdateAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferId} rejected successfully with reason {Reason}",
                id,
                reason);
        }

        public async Task CancelTransferAsync(
            int id)
        {
            var transfer =
                await _transferRepository
                    .GetByIdAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Transfer not found.");
            }

            if (transfer.Status !=
                TransferStatus.Pending)
            {
                throw new ConflictException(
                    "Only pending transfers can be cancelled.");
            }

            transfer.Status =
                TransferStatus.Cancelled;

            await _transferRepository
                .UpdateAsync(transfer);
        }

        public async Task CreateTransferAsync(
             CreateWarehouseTransferDto request)
        {
            _logger.LogInformation(
                "Creating warehouse transfer from source warehouse {SourceWarehouseId} to destination warehouse {DestinationWarehouseId}",
                request.SourceWarehouseId,
                request.DestinationWarehouseId);

            if (request.SourceWarehouseId ==
                request.DestinationWarehouseId)
            {
                throw new BadRequestException(
                    "Source and destination warehouse cannot be the same.");
            }

            var sourceWarehouse =
                await _warehouseRepository
                    .GetByIdAsync(
                        request.SourceWarehouseId);

            if (sourceWarehouse == null ||
                !sourceWarehouse.IsActive)
            {
                throw new NotFoundException(
                    "Source warehouse not found.");
            }

            var destinationWarehouse =
                await _warehouseRepository
                    .GetByIdAsync(
                        request.DestinationWarehouseId);

            if (destinationWarehouse == null ||
                !destinationWarehouse.IsActive)
            {
                throw new NotFoundException(
                    "Destination warehouse not found.");
            }

            if(_currentUserService.Role == "WarehouseManager")
            {
                if(_currentUserService.AssignedWarehouseId !=
                    request.SourceWarehouseId &&
                    _currentUserService.AssignedWarehouseId !=
                    request.DestinationWarehouseId)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted unauthorized creation of transfer from {SourceId} to {DestId}",
                        _currentUserService.UserId,
                        request.SourceWarehouseId,
                        request.DestinationWarehouseId);
                    throw new ForbiddenException(
                        "You are not authorized to create transfers for other warehouses.");
                }
            }

            if (!request.Items.Any())
            {
                throw new BadRequestException(
                    "At least one item is required.");
            }

            var duplicateProducts =
                request.Items
                    .GroupBy(i => i.ProductId)
                    .Any(g => g.Count() > 1);

            if (duplicateProducts)
            {
                throw new BadRequestException(
                    "Duplicate products are not allowed.");
            }

            decimal transferVolume = 0;

            foreach (var item in request.Items)
            {
                var product =
                    await _productRepository
                        .GetByIdAsync(
                            item.ProductId);
                if (product == null || !product.IsActive)
                {
                    throw new NotFoundException(
                        $"Product with ID {item.ProductId} not found.");
                }

                if (product.RequiredStorageType !=
                    destinationWarehouse.StorageType)
                {
                    throw new BadRequestException(
                        $"{product.Name} cannot be stored in {destinationWarehouse.StorageType}.");
                }

                var inventory =
                    await _inventoryRepository
                        .GetInventoryAsync(
                            request.SourceWarehouseId,
                            item.ProductId);

                if (inventory == null)
                {
                    throw new BadRequestException(
                        $"Product {product!.Name} is not available in the source warehouse.");
                }

                if (inventory.Quantity <
                    item.Quantity)
                {
                    throw new BadRequestException(
                        $"Insufficient inventory for product {product!.Name}.");
                }

                transferVolume +=
                    product!.Length *
                    product.Width *
                    product.Height *
                    item.Quantity;
            }
            
            var effectiveCapacity =
                destinationWarehouse.AvailableCapacity -
                destinationWarehouse.ReservedCapacity;

            if (transferVolume >
                effectiveCapacity)
            {
                _logger.LogWarning(
                    "Destination warehouse {WarehouseId} capacity exceeded for transfer total volume {TotalVolume}",
                    request.DestinationWarehouseId,
                    transferVolume);
                throw new BadRequestException(
                    "Insufficient destination warehouse capacity.");
            }

            var transfer = new WarehouseTransfer
            {
                TransferNumber =
                    $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}",

                SourceWarehouseId =
                    request.SourceWarehouseId,

                DestinationWarehouseId =
                    request.DestinationWarehouseId,

                CreatedByUserId =
                    _currentUserService.UserId,

                Reason =
                    request.Reason,

                Status =
                    TransferStatus.Pending,

                WarehouseTransferItems =
                    request.Items
                        .Select(i =>
                            new WarehouseTransferItem
                            {
                                ProductId =
                                    i.ProductId,

                                Quantity =
                                    i.Quantity
                            })
                        .ToList(),
                TransferVolume = transferVolume
            };

            await _transferRepository
                .AddAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferNumber} created successfully",
                transfer.TransferNumber);
        }

        public async Task<WarehouseTransferResponseDto> GetTransferByIdAsync(int id)
        {
            var transfer =
                await _transferRepository
                    .GetTransferWithDetailsAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Warehouse transfer not found.");
            }

            return _mapper.Map<
                WarehouseTransferResponseDto>(
                    transfer);
        }

        public async Task<WarehouseTransferResponseDto> GetByTransferNumberAsync(string transferNumber)
        {
            var transfer =
                await _transferRepository
                    .GetByTransferNumberAsync(transferNumber);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Warehouse transfer not found.");
            }

            if(_currentUserService.Role =="WarehouseManager")
            {
                if(_currentUserService.AssignedWarehouseId !=
                    transfer.SourceWarehouseId &&
                    _currentUserService.AssignedWarehouseId !=
                    transfer.DestinationWarehouseId)
                {
                    throw new ForbiddenException(
                        "You are not authorized to view this transfer.");
                }
            }

            return _mapper.Map<WarehouseTransferResponseDto>(transfer);
        }

       public async Task<PagedResponseDto<WarehouseTransferResponseDto>>GetTransfersAsync(
                PaginationParams pagination,WarehouseTransferFilterDto filter)
        {
            var transfers =
                await _transferRepository
                    .GetTransfersWithDetailsAsync();

            if (_currentUserService.Role == "WarehouseManager")
            {
                var warehouseId =
                    _currentUserService.AssignedWarehouseId;

                transfers = transfers.Where(
                    t =>
                        t.SourceWarehouseId == warehouseId ||
                        t.DestinationWarehouseId == warehouseId);
            }

            if (filter.Status.HasValue)
            {
                transfers = transfers.Where(
                    t => t.Status ==
                        filter.Status.Value);
            }

            if (filter.SourceWarehouseId.HasValue)
            {
                transfers = transfers.Where(
                    t => t.SourceWarehouseId ==
                        filter.SourceWarehouseId.Value);
            }

            if (filter.DestinationWarehouseId.HasValue)
            {
                transfers = transfers.Where(
                    t => t.DestinationWarehouseId ==
                        filter.DestinationWarehouseId.Value);
            }

            var totalRecords =
                transfers.Count();

            var pagedTransfers =
                transfers
                    .Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                WarehouseTransferResponseDto>
            {
                Data =
                    _mapper.Map<
                        IEnumerable<
                            WarehouseTransferResponseDto>>(
                                pagedTransfers),

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

        public async Task MarkReceivedAsync(int id)
        {
            _logger.LogInformation(
                "Marking warehouse transfer {TransferId} as Received",
                id);

            var transfer =
                await _transferRepository
                    .GetTransferWithDetailsAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Transfer not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" && _currentUserService.AssignedWarehouseId !=
                transfer.DestinationWarehouseId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted unauthorized receive of transfer {TransferId} for warehouse {WarehouseId}",
                    _currentUserService.UserId,
                    id,
                    transfer.DestinationWarehouseId);
                throw new ForbiddenException(
                    "You can only receive transfers for your warehouse.");
            }

            if (transfer.Status !=
                TransferStatus.InTransit)
            {
                throw new ConflictException(
                    "Only transfers in transit can be received.");
            }

            transfer.Status =TransferStatus.Received;

            await _transferRepository.UpdateAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferId} marked as Received successfully",
                id);
        }


        public async Task MarkInTransitAsync(
            int id)
        {
            _logger.LogInformation(
                "Marking warehouse transfer {TransferId} as InTransit",
                id);

            var transfer =
                await _transferRepository
                    .GetTransferWithDetailsAsync(id);

            if (transfer == null)
            {
                throw new NotFoundException(
                    "Transfer not found.");
            }

            if (transfer.Status !=
                TransferStatus.Approved)
            {
                throw new ConflictException(
                    "Only approved transfers can be marked in transit.");
            }

            transfer.Status =
                TransferStatus.InTransit;

            await _transferRepository
                .UpdateAsync(transfer);

            _logger.LogInformation(
                "Warehouse transfer {TransferId} marked as InTransit successfully",
                id);
        }
    }
}