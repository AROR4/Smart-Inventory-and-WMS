using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(
            IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateWarehouse(
            [FromBody] CreateWarehouseDto request)
        {
            await _warehouseService
                .CreateWarehouseAsync(request);

            return Ok(new
            {
                Message = "Warehouse created successfully."
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var warehouses = await _warehouseService
                .GetAllWarehousesAsync();

            return Ok(warehouses);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> GetWarehouseById(
            int id)
        {
            var warehouse = await _warehouseService
                .GetWarehouseByIdAsync(id);

            return Ok(warehouse);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWarehouse(
            int id,
            [FromBody] UpdateWarehouseDto request)
        {
            await _warehouseService
                .UpdateWarehouseAsync(id, request);

            return Ok(new
            {
                Message = "Warehouse updated successfully."
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteWarehouse(
            int id)
        {
            await _warehouseService
                .DeleteWarehouseAsync(id);

            return Ok(new
            {
                Message = "Warehouse deleted successfully."
            });
        }

        
    }
}