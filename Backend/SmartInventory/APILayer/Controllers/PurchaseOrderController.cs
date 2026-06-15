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
    public class PurchaseOrderController
        : ControllerBase
    {
        private readonly
            IPurchaseOrderService
            _purchaseOrderService;

        public PurchaseOrderController(
            IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService =
                purchaseOrderService;
        }

        [HttpPost]
        [Authorize(
            Roles =
            "Admin,WarehouseManager")]
        public async Task<IActionResult>
            CreatePurchaseOrder(
                CreatePurchaseOrderDto request)
        {
            await _purchaseOrderService
                .CreatePurchaseOrderAsync(
                    request);

            return Ok(
                new
                {
                    Message =
                        "Purchase order created successfully."
                });
        }

        [HttpPut("{id}/approve")]
        [Authorize(
            Roles =
            "Admin")]
        public async Task<IActionResult>
            ApprovePurchaseOrder(
                int id)
        {
            await _purchaseOrderService
                .ApprovePurchaseOrderAsync(
                    id);

            return Ok(
                new
                {
                    Message =
                        "Purchase order approved successfully."
                });
        }

        [HttpPut("{id}/reject")]
        [Authorize(
            Roles =
            "Admin")]
        public async Task<IActionResult>
            RejectPurchaseOrder(
                int id,
                RejectPurchaseOrderDto request)
        {
            await _purchaseOrderService
                .RejectPurchaseOrderAsync(
                    id,
                    request.Reason);

            return Ok(
                new
                {
                    Message =
                        "Purchase order rejected successfully."
                });
        }

        [HttpPut("{id}/receive")]
        [Authorize(
            Roles =
            "WarehouseManager")]
        public async Task<IActionResult>
            ReceivePurchaseOrder(
                int id,
                ReceivePurchaseOrderDto request)
        {
            await _purchaseOrderService
                .ReceivePurchaseOrderAsync(
                    id,
                    request.InvoiceNumber);

            return Ok(
                new
                {
                    Message =
                        "Purchase order received successfully."
                });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetPurchaseOrderById(
                int id)
        {
            var purchaseOrder =
                await _purchaseOrderService
                    .GetPurchaseOrderByIdAsync(
                        id);

            return Ok(
                purchaseOrder);
        }

        [HttpGet("number/{orderNumber}")]
        public async Task<IActionResult>
            GetByOrderNumber(
                string orderNumber)
        {
            var purchaseOrder =
                await _purchaseOrderService
                    .GetByOrderNumberAsync(
                        orderNumber);

            return Ok(
                purchaseOrder);
        }

        [HttpGet]
        public async Task<IActionResult>
            GetPurchaseOrders(
                [FromQuery]
                PaginationParams pagination,
                [FromQuery]
                PurchaseOrderFilterDto filter)
        {
            var result =
                await _purchaseOrderService
                    .GetPurchaseOrdersAsync(
                        pagination,
                        filter);

            return Ok(
                result);
        }
    }
}