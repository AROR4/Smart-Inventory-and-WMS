using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Supplier")]
    public class SupplierOrderController
        : ControllerBase
    {
        private readonly
            ISupplierOrderService
            _supplierOrderService;

        public SupplierOrderController(
            ISupplierOrderService supplierOrderService)
        {
            _supplierOrderService =
                supplierOrderService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult>
            GetPendingOrders(
                [FromQuery]
                PaginationParams pagination)
        {
            var result =
                await _supplierOrderService
                    .GetPendingOrdersAsync(
                        pagination);

            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult>
            GetOrderHistory(
                [FromQuery]
                PaginationParams pagination)
        {
            var result =
                await _supplierOrderService
                    .GetOrderHistoryAsync(
                        pagination);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetOrderDetails(
                int id)
        {
            var result =
                await _supplierOrderService
                    .GetOrderDetailsAsync(id);

            return Ok(result);
        }

        [HttpPut("{id}/ship")]
        public async Task<IActionResult>
            MarkOrderShipped(
                int id)
        {
            await _supplierOrderService
                .MarkOrderShippedAsync(id);

            return Ok(
                new
                {
                    Message =
                        "Order marked as shipped successfully."
                });
        }
    }
}