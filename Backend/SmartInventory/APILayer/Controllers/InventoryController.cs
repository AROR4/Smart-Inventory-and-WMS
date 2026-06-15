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
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(
            IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPut("adjust-stock")]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> AdjustStock(
            AdjustStockDto request)
        {
            await _inventoryService
                .AdjustStockAsync(request);

            return Ok(new
            {
                Message = "Stock adjusted successfully."
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,WarehouseManager,InventoryStaff")]        
        public async Task<IActionResult> GetInventory(
            [FromQuery] PaginationParams pagination,
            [FromQuery] InventoryFilterDto filter)
        {
            var result =
                await _inventoryService
                    .GetInventoryAsync(
                        pagination,
                        filter);

            return Ok(result);
        }
    }
}