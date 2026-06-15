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
    public class LowStockAlertController : ControllerBase
    {
        private readonly ILowStockAlertService _lowStockAlertService;

        public LowStockAlertController(
            ILowStockAlertService lowStockAlertService)
        {
            _lowStockAlertService = lowStockAlertService;
        }

        
        [HttpGet]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult>
            GetAlerts(
                [FromQuery]
                PaginationParams pagination)
        {
            var result =
                await _lowStockAlertService
                    .GetActiveAlertsAsync(
                        pagination);

            return Ok(result);
        }
    }
}