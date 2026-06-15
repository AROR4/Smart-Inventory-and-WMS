using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockMovementController
        : ControllerBase
    {
        private readonly
            IStockMovementService
            _stockMovementService;

        public StockMovementController(
            IStockMovementService
            stockMovementService)
        {
            _stockMovementService =
                stockMovementService;
        }

        [HttpGet]
        [Authorize(Roles ="Admin,WarehouseManager,InventoryStaff")]
        public async Task<IActionResult> GetStockMovements(
                [FromQuery]
                PaginationParams pagination,

                [FromQuery]
                StockMovementFilterDto filter)
        {
            var result =
                await _stockMovementService
                    .GetStockMovementsAsync(
                        pagination,
                        filter);

            return Ok(result);
        }
    }
}