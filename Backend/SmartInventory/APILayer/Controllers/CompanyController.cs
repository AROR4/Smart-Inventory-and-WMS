using Microsoft.AspNetCore.Mvc;
using SmartInventoryManagement.BusinessLayer.Interfaces;

namespace SmartInventoryManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController
        : ControllerBase
    {
        private readonly
            ICompanyService
            _companyService;

        public CompanyController(
            ICompanyService companyService)
        {
            _companyService =
                companyService;
        }

        [HttpPost]
        public async Task<IActionResult>
            Create(
                CreateCompanyDto request)
        {
            await _companyService
                .CreateCompanyAsync(
                    request);

            return Ok(new { Message = "Company created successfully." });
        }

        [HttpGet]
        public async Task<IActionResult>
            GetAll()
        {
            return Ok(
                await _companyService
                    .GetCompaniesAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult>
            GetById(
                int id)
        {
            return Ok(
                await _companyService
                    .GetCompanyByIdAsync(
                        id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult>
            Update(
                int id,
                UpdateCompanyDto request)
        {
            await _companyService
                .UpdateCompanyAsync(
                    id,
                    request);

            return Ok(new { Message = "Company updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>
            Delete(
                int id)
        {
            await _companyService
                .DeleteCompanyAsync(
                    id);

            return Ok(new { Message = "Company deleted successfully." });
        }
}
}