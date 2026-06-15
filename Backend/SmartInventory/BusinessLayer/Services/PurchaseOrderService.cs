using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IRepository<Supplier> _supplierRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PurchaseOrderService> _logger;
        private readonly IMapper _mapper;

        public PurchaseOrderService(
            IPurchaseOrderRepository purchaseOrderRepository,
            IRepository<Supplier> supplierRepository,
            IRepository<Warehouse> warehouseRepository,
            IProductRepository productRepository,
            ICurrentUserService currentUserService,
            ApplicationDbContext context,
            ILogger<PurchaseOrderService> logger,
            IMapper mapper)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _supplierRepository = supplierRepository;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task CreatePurchaseOrderAsync(
            CreatePurchaseOrderDto request)
        {
            _logger.LogInformation(
                "Creating purchase order for supplier {SupplierId} and warehouse {WarehouseId}",
                request.SupplierId,
                request.WarehouseId);

            var supplier =
                await _supplierRepository
                    .GetByIdAsync(request.SupplierId);

            if (supplier == null || !supplier.IsActive)
            {
                throw new NotFoundException(
                    "Supplier not found.");
            }

            var warehouse =
                await _warehouseRepository
                    .GetByIdAsync(request.WarehouseId);

            if (warehouse == null || !warehouse.IsActive)
            {
                throw new NotFoundException(
                    "Warehouse not found.");
            }
            
            if(_currentUserService.Role =="WarehouseManager" && _currentUserService.AssignedWarehouseId != request.WarehouseId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted unauthorized access to create purchase order for warehouse {WarehouseId}",
                    _currentUserService.UserId,
                    request.WarehouseId);
                throw new ForbiddenException(
                    "You are not authorized to create purchase orders for other warehouse .");
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

            var purchaseOrderItems =
                new List<PurchaseOrderItem>();

            decimal totalVolume = 0;

            foreach (var item in request.Items)
            {
                var product =
                    await _productRepository
                        .GetByIdAsync(item.ProductId);

                if (product == null ||
                    !product.IsActive)
                {
                    _logger.LogWarning(
                        "Product {ProductId} not found or inactive when creating purchase order",
                        item.ProductId);
                    throw new NotFoundException(
                        $"Product {item.ProductId} not found.");
                }

                if (product.RequiredStorageType != warehouse.StorageType)
                {
                    _logger.LogWarning(
                        "Product {ProductId} storage type {ProductStorageType} is incompatible with warehouse {WarehouseId} storage type {WarehouseStorageType}",
                        product.Id,
                        product.RequiredStorageType,
                        warehouse.Id,
                        warehouse.StorageType);
                    throw new BadRequestException(
                        $"Product {product.Name} requires {product.RequiredStorageType} storage, but warehouse {warehouse.Name} has {warehouse.StorageType}.");
                }

                totalVolume +=
                    product.Length *
                    product.Width *
                    product.Height *
                    item.Quantity;

                purchaseOrderItems.Add(
                    new PurchaseOrderItem
                    {
                        ProductId =
                            product.Id,

                        OrderedQuantity =
                            item.Quantity,

                        ReceivedQuantity = 0,

                        UnitPrice =
                            product.UnitPrice
                    });
            }

            var status =
                _currentUserService.Role == "Admin"
                    ? PurchaseOrderStatus.Ordered
                    : PurchaseOrderStatus.PendingApproval;

            if (status == PurchaseOrderStatus.Ordered)
            {
                var effectiveCapacity =
                    warehouse.AvailableCapacity -
                    warehouse.ReservedCapacity;

                if (totalVolume > effectiveCapacity)
                {
                    _logger.LogWarning(
                        "Warehouse {WarehouseId} capacity exceeded for purchase order total volume {TotalVolume}",
                        request.WarehouseId,
                        totalVolume);
                    throw new BadRequestException(
                        "Insufficient warehouse capacity.");
                }

                warehouse.ReservedCapacity +=
                    totalVolume;

                await _warehouseRepository
                    .UpdateAsync(warehouse);
            }

            var purchaseOrder =
                new PurchaseOrder
                {
                    OrderNumber =
                        $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",

                    SupplierId =
                        request.SupplierId,

                    WarehouseId =
                        request.WarehouseId,

                    CreatedByUserId =
                        _currentUserService.UserId,

                    Status =
                        status,

                    TotalVolume =
                        totalVolume,

                    PurchaseOrderItems =
                        purchaseOrderItems
                };

            await _purchaseOrderRepository
                .AddAsync(purchaseOrder);

            _logger.LogInformation(
                "Purchase order {OrderNumber} created successfully with status {Status}",
                purchaseOrder.OrderNumber,
                purchaseOrder.Status);
        }


        public async Task ApprovePurchaseOrderAsync(
            int id)
        {
            _logger.LogInformation(
                "User {UserId} approving purchase order {PurchaseOrderId}",
                _currentUserService.UserId,
                id);

            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetByIdAsync(id);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (purchaseOrder.Status !=
                PurchaseOrderStatus.PendingApproval)
            {
                throw new ConflictException(
                    "Only pending purchase orders can be approved.");
            }

            var warehouse =
                await _warehouseRepository
                    .GetByIdAsync(
                        purchaseOrder.WarehouseId);

            if (warehouse == null)
            {
                throw new NotFoundException(
                    "Warehouse not found.");
            }

            var effectiveCapacity =
                warehouse.AvailableCapacity -
                warehouse.ReservedCapacity;

            if (purchaseOrder.TotalVolume >
                effectiveCapacity)
            {
                _logger.LogWarning(
                    "Warehouse {WarehouseId} capacity exceeded for purchase order {PurchaseOrderId}",
                    purchaseOrder.WarehouseId,
                    purchaseOrder.Id);
                throw new BadRequestException(
                    "Insufficient warehouse capacity.");
            }

            warehouse.ReservedCapacity +=
                purchaseOrder.TotalVolume;

            purchaseOrder.Status =
                PurchaseOrderStatus.Ordered;

            await _warehouseRepository
                .UpdateAsync(warehouse);

            await _purchaseOrderRepository
                .UpdateAsync(purchaseOrder);

            _logger.LogInformation(
                "Purchase order {PurchaseOrderId} approved successfully",
                id);
        }

        public async Task RejectPurchaseOrderAsync(
            int id,
            string reason)
        {
            _logger.LogInformation(
                "User {UserId} rejecting purchase order {PurchaseOrderId}",
                _currentUserService.UserId,
                id);

            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetByIdAsync(id);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (purchaseOrder.Status !=
                PurchaseOrderStatus.PendingApproval)
            {
                throw new ConflictException(
                    "Only pending purchase orders can be rejected.");
            }

            purchaseOrder.Status =
                PurchaseOrderStatus.Rejected;

            purchaseOrder.RejectionReason =
                reason;

            await _purchaseOrderRepository
                .UpdateAsync(purchaseOrder);

            _logger.LogInformation(
                "Purchase order {PurchaseOrderId} rejected successfully with reason {Reason}",
                id,
                reason);
        }

        public async Task ReceivePurchaseOrderAsync(
            int id,
            string invoiceNumber)
        {
            _logger.LogInformation(
                "User {UserId} receiving purchase order {PurchaseOrderId} with invoice {InvoiceNumber}",
                _currentUserService.UserId,
                id,
                invoiceNumber);

            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetByIdAsync(id);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" && _currentUserService.AssignedWarehouseId !=
                purchaseOrder.WarehouseId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted unauthorized receiving of purchase order {PurchaseOrderId} for warehouse {WarehouseId}",
                    _currentUserService.UserId,
                    id,
                    purchaseOrder.WarehouseId);
                throw new ForbiddenException(
                    "You can only receive purchase orders for your warehouse.");
            }

            if (purchaseOrder.Status !=
                PurchaseOrderStatus.Shipped)
            {
                throw new ConflictException(
                    "Only shipped purchase orders can be received.");
            }

            if (string.IsNullOrWhiteSpace(
                    invoiceNumber))
            {
                throw new BadRequestException(
                    "Invoice number is required.");
            }

            purchaseOrder.InvoiceNumber =
                invoiceNumber;

            purchaseOrder.ReceivedDate =
                DateTime.Now;

            purchaseOrder.Status =
                PurchaseOrderStatus.Received;

            await _purchaseOrderRepository
                .UpdateAsync(purchaseOrder);

            _logger.LogInformation(
                "Purchase order {PurchaseOrderId} received successfully",
                id);
        }

        public async Task<PurchaseOrderResponseDto>
            GetPurchaseOrderByIdAsync(
                int id)
        {
            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetPurchaseOrderWithDetailsAsync(id);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" && _currentUserService.AssignedWarehouseId !=
                purchaseOrder.WarehouseId)
            {
                throw new ForbiddenException(
                    "You can only view purchase orders for your warehouse.");
            }

            return _mapper.Map<
                PurchaseOrderResponseDto>(
                    purchaseOrder);
        }

        public async Task<PurchaseOrderResponseDto>
            GetByOrderNumberAsync(
                string orderNumber)
        {
            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetByOrderNumberAsync(
                        orderNumber);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            return _mapper.Map<
                PurchaseOrderResponseDto>(
                    purchaseOrder);
        }


        public async Task CompletePurchaseOrderAsync(int id)
        {
            _logger.LogInformation(
                "Completing purchase order {PurchaseOrderId}",
                id);

            var purchaseOrder =
                await _purchaseOrderRepository
                    .GetByIdAsync(id);

            if (purchaseOrder == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (purchaseOrder.Status !=
                PurchaseOrderStatus.Received)
            {
                throw new ConflictException(
                    "Only received purchase orders can be completed.");
            }

            var warehouse =
                await _warehouseRepository
                    .GetByIdAsync(
                        purchaseOrder.WarehouseId);

            if (warehouse == null)
            {
                throw new NotFoundException(
                    "Warehouse not found.");
            }

            warehouse.ReservedCapacity -=
                purchaseOrder.TotalVolume;

            if (warehouse.ReservedCapacity < 0)
            {
                warehouse.ReservedCapacity = 0;
            }

            purchaseOrder.Status =
                PurchaseOrderStatus.Completed;

            await _warehouseRepository
                .UpdateAsync(warehouse);

            await _purchaseOrderRepository
                .UpdateAsync(purchaseOrder);

            _logger.LogInformation(
                "Purchase order {PurchaseOrderId} completed successfully",
                id);
        }

        public async Task<
            PagedResponseDto<
                PurchaseOrderResponseDto>>
            GetPurchaseOrdersAsync(
                PaginationParams pagination,
                PurchaseOrderFilterDto filter)
        {
            var purchaseOrders =
                await _purchaseOrderRepository
                    .GetPurchaseOrdersWithDetailsAsync();

            if (_currentUserService.Role == "WarehouseManager")
            {
                purchaseOrders = purchaseOrders.Where(
                    p => p.WarehouseId ==
                         _currentUserService.AssignedWarehouseId);
            }

            if (filter.Status.HasValue)
            {
                purchaseOrders = purchaseOrders.Where(
                    po => po.Status ==
                        filter.Status.Value);
            }

            if (filter.SupplierId.HasValue)
            {
                purchaseOrders = purchaseOrders.Where(
                    po => po.SupplierId ==
                        filter.SupplierId.Value);
            }

            if (filter.WarehouseId.HasValue)
            {
                purchaseOrders = purchaseOrders.Where(
                    po => po.WarehouseId ==
                        filter.WarehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                purchaseOrders = purchaseOrders.Where(
                    po =>
                        po.OrderNumber.Contains(
                            filter.Search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        po.Supplier.Name.Contains(
                            filter.Search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        po.Warehouse.Name.Contains(
                            filter.Search,
                            StringComparison.OrdinalIgnoreCase));
            }

            var totalRecords =
                purchaseOrders.Count();

            var pagedPurchaseOrders =
                purchaseOrders
                    .Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                PurchaseOrderResponseDto>
            {
                Data =
                    _mapper.Map<
                        IEnumerable<
                            PurchaseOrderResponseDto>>(
                                pagedPurchaseOrders),

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