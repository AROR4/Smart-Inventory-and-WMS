using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class SupplierOrderService : ISupplierOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        private readonly ICurrentUserService _currentUserService;

        private readonly IMapper _mapper;

        public SupplierOrderService(
            IPurchaseOrderRepository purchaseOrderRepository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }


        public async Task<
            PagedResponseDto<
                PurchaseOrderResponseDto>>
            GetPendingOrdersAsync(
                PaginationParams pagination)
        {
            if (!_currentUserService
                    .SupplierId
                    .HasValue)
            {
                throw new ForbiddenException(
                    "Supplier account required.");
            }

            var orders =
                await _purchaseOrderRepository
                    .GetSupplierPendingOrdersAsync(
                        _currentUserService
                            .SupplierId.Value);

            var totalRecords =
                orders.Count();

            var pagedOrders =
                orders
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
                                pagedOrders),

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

        public async Task<
            PagedResponseDto<
                PurchaseOrderResponseDto>>
            GetOrderHistoryAsync(
                PaginationParams pagination)
        {
            if (!_currentUserService
                    .SupplierId
                    .HasValue)
            {
                throw new ForbiddenException(
                    "Supplier account required.");
            }

            var orders =
                await _purchaseOrderRepository
                    .GetSupplierOrderHistoryAsync(
                        _currentUserService
                            .SupplierId.Value);

            var totalRecords =
                orders.Count();

            var pagedOrders =
                orders
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
                                pagedOrders),

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

        public async Task<
            PurchaseOrderResponseDto>
            GetOrderDetailsAsync(
                int id)
        {
            if (!_currentUserService
                    .SupplierId
                    .HasValue)
            {
                throw new ForbiddenException(
                    "Supplier account required.");
            }

            var order =
                await _purchaseOrderRepository
                    .GetPurchaseOrderWithDetailsAsync(
                        id);

            if (order == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (order.SupplierId !=
                _currentUserService
                    .SupplierId.Value)
            {
                throw new ForbiddenException(
                    "You cannot access this order.");
            }

            return _mapper.Map<
                PurchaseOrderResponseDto>(
                    order);
        }

        public async Task
            MarkOrderShippedAsync(
                int id)
        {
            if (!_currentUserService
                    .SupplierId
                    .HasValue)
            {
                throw new ForbiddenException(
                    "Supplier account required.");
            }

            var order =
                await _purchaseOrderRepository
                    .GetPurchaseOrderWithDetailsAsync(
                        id);

            if (order == null)
            {
                throw new NotFoundException(
                    "Purchase order not found.");
            }

            if (order.SupplierId !=
                _currentUserService
                    .SupplierId.Value)
            {
                throw new ForbiddenException(
                    "You cannot modify this order.");
            }

            if (order.Status !=
                PurchaseOrderStatus.Ordered)
            {
                throw new ConflictException(
                    "Only ordered purchase orders can be shipped.");
            }

            order.Status =
                PurchaseOrderStatus.Shipped;

            await _purchaseOrderRepository
                .UpdateAsync(order);
        }

    }
}