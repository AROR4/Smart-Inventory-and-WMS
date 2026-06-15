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
    public class WarehouseTaskController : ControllerBase
    {
        private readonly IWarehouseTaskService _warehouseTaskService;

        public WarehouseTaskController(
            IWarehouseTaskService warehouseTaskService)
        {
            _warehouseTaskService = warehouseTaskService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,WarehouseManager")]
        public async Task<IActionResult> CreateTask(
            [FromBody] CreateWarehouseTaskDto request)
        {
            await _warehouseTaskService
                .CreateTaskAsync(request);

            return Ok(new
            {
                Message = "Task created successfully."
            });
        }

        [HttpPut("{id}/start")]
        [Authorize(Roles = "InventoryStaff")]
        public async Task<IActionResult> StartTask(
            int id)
        {
            await _warehouseTaskService
                .StartTaskAsync(id);

            return Ok(new
            {
                Message = "Task started successfully."
            });
        }

        [HttpPut("{id}/complete")]
        [Authorize(Roles = "InventoryStaff")]
        public async Task<IActionResult> CompleteTask(
            int id)
        {
            await _warehouseTaskService
                .CompleteTaskAsync(id);

            return Ok(new
            {
                Message = "Task completed successfully."
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles =
            "Admin,WarehouseManager,InventoryStaff")]
        public async Task<IActionResult> GetTaskById(
            int id)
        {
            var task =
                await _warehouseTaskService
                    .GetTaskByIdAsync(id);

            return Ok(task);
        }

        [HttpGet]
        [Authorize(Roles =
            "Admin,WarehouseManager,InventoryStaff")]
        public async Task<IActionResult> GetTasks(
            [FromQuery] PaginationParams pagination,
            [FromQuery] WarehouseTaskFilterDto filter)
        {
            var result =
                await _warehouseTaskService
                    .GetTasksAsync(
                        pagination,
                        filter);

            return Ok(result);
        }
    }
}