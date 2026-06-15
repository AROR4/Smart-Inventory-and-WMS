using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(
            IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto request)
        {
            await _userService.CreateUserAsync(request);

            return Ok(new
            {
                Message = "User created successfully."

            });
        }

        // [HttpGet]
        // public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams pagination)
        // {
        //     var result =await _userService.GetUsersAsync(pagination);

        //     return Ok(result);
        // }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(
            int id)
        {
            var user = await _userService
                .GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    Message = "User not found."
                });
            }

            return Ok(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] PaginationParams pagination,
            [FromQuery] string? role)
        {
            return Ok(
                await _userService.GetUsersAsync(
                    pagination,
                    role));
        }

        [HttpPost("{id}/resend-invite")]
        public async Task<IActionResult> ResendInvite(
            int id)
        {            await _userService.ResendInviteAsync(id);  

            return Ok(new
            {
                Message = "Invite resent successfully."
            });         
        }
}

}