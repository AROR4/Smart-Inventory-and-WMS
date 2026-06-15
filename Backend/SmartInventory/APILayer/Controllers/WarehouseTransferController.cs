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
    public class WarehouseTransferController
        : ControllerBase
    {
        private readonly
            IWarehouseTransferService
            _transferService;

        public WarehouseTransferController(
            IWarehouseTransferService transferService)
        {
            _transferService =
                transferService;
        }

        [HttpPost]
        [Authorize(
            Roles =
            "WarehouseManager")]
        public async Task<IActionResult>
            CreateTransfer(
                CreateWarehouseTransferDto request)
        {
            await _transferService
                .CreateTransferAsync(
                    request);

            return Ok(
                new
                {
                    Message =
                        "Transfer created successfully."
                });
        }

        [HttpPut("{id}/approve")]
        [Authorize(
            Roles =
            "Admin")]
        public async Task<IActionResult>
            ApproveTransfer(
                int id)
        {
            await _transferService
                .ApproveTransferAsync(
                    id);

            return Ok(
                new
                {
                    Message =
                        "Transfer approved successfully."
                });
        }

        [HttpPut("{id}/reject")]
        [Authorize(
            Roles =
            "Admin")]
        public async Task<IActionResult>
            RejectTransfer(
                int id,
                RejectTransferDto request)
        {
            await _transferService
                .RejectTransferAsync(
                    id,
                    request.Reason);

            return Ok(
                new
                {
                    Message =
                        "Transfer rejected successfully."
                });
        }

        [HttpPut("{id}/receive")]
        [Authorize(
            Roles =
            "WarehouseManager")]
        public async Task<IActionResult>
            MarkReceived(
                int id)
        {
            await _transferService
                .MarkReceivedAsync(
                    id);

            return Ok(
                new
                {
                    Message =
                        "Transfer received successfully."
                });
        }

        [HttpPut("{id}/cancel")]
        [Authorize(
            Roles =
            "Admin")]
        public async Task<IActionResult>
            CancelTransfer(
                int id)
        {
            await _transferService
                .CancelTransferAsync(
                    id);

            return Ok(
                new
                {
                    Message =
                        "Transfer cancelled successfully."
                });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetTransferById(
                int id)
        {
            var transfer =
                await _transferService
                    .GetTransferByIdAsync(
                        id);

            return Ok(
                transfer);
        }

        [HttpGet]
        public async Task<IActionResult>
            GetTransfers(
                [FromQuery]
                PaginationParams pagination,

                [FromQuery]
                WarehouseTransferFilterDto filter)
        {
            var result =
                await _transferService
                    .GetTransfersAsync(
                        pagination,
                        filter);

            return Ok(
                result);
        }

        [HttpGet("number/{transferNumber}")]
        public async Task<IActionResult>
            GetByTransferNumber(
                string transferNumber)
        {
            var transfer =
                await _transferService
                    .GetByTransferNumberAsync(
                        transferNumber);

            return Ok(
                transfer);
        }
    }
}