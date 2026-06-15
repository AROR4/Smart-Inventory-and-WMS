using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;

namespace SmartInventoryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(
            IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult>
            CreateProduct(
                CreateProductDto request)
        {
            await _productService
                .CreateProductAsync(request);

            return Ok(new
            {
                Message =
                    "Product created successfully."
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult>
            UpdateProduct(
                int id,
                UpdateProductDto request)
        {
            await _productService
                .UpdateProductAsync(
                    id,
                    request);

            return Ok(new
            {
                Message =
                    "Product updated successfully."
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>
            DeleteProduct(
                int id)
        {
            await _productService
                .DeleteProductAsync(id);

            return Ok(new
            {
                Message =
                    "Product deleted successfully."
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetProductById(
                int id)
        {
            var product =
                await _productService
                    .GetProductByIdAsync(id);

            return Ok(product);
        }

        [HttpGet]
        public async Task<IActionResult>
            GetProducts(
                [FromQuery]
                PaginationParams pagination,

                [FromQuery]
                ProductFilterDto filter)
        {
            var products =
                await _productService
                    .GetProductsAsync(
                        pagination,
                        filter);

            return Ok(products);
        }
    }
}