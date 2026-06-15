using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IMapper _mapper;

        public StockMovementService(IStockMovementRepository stockMovementRepository, IMapper mapper)
        {
            _stockMovementRepository = stockMovementRepository;
            _mapper = mapper;
        }

        public async Task<PagedResponseDto<StockMovementResponseDto>>
            GetStockMovementsAsync(PaginationParams pagination, StockMovementFilterDto filter)
        {
            var movements =
                await _stockMovementRepository
                    .GetStockMovementsWithDetailsAsync();

            if (filter.ProductId.HasValue)
            {
                movements = movements.Where(
                    sm => sm.ProductId ==
                        filter.ProductId.Value);
            }

            if (filter.WarehouseId.HasValue)
            {
                movements = movements.Where(
                    sm => sm.WarehouseId ==
                        filter.WarehouseId.Value);
            }

            if (filter.Type.HasValue)
            {
                movements = movements.Where(
                    sm => sm.Type ==
                        filter.Type.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                movements = movements.Where(sm =>
                    sm.Product.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    sm.Warehouse.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    sm.CreatedByUser.Name.Contains(
                        filter.Search,
                        StringComparison.OrdinalIgnoreCase));
            }

            var totalRecords =
                movements.Count();

            var pagedMovements =
                movements
                    .Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                StockMovementResponseDto>
            {
                Data = _mapper.Map<
                    IEnumerable<
                        StockMovementResponseDto>>(
                            pagedMovements),

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

