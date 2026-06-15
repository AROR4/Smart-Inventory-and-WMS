using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IRepository<Company> _companyRepository;

        private readonly IMapper _mapper;

        public CompanyService(IRepository<Company> companyRepository,IMapper mapper)
        {
            _companyRepository =
                companyRepository;

            _mapper =
                mapper;
        }

        public async Task CreateCompanyAsync(
            CreateCompanyDto request)
        {
            var companies =
                await _companyRepository
                    .GetAllAsync();

            var existingCompany =
                companies.FirstOrDefault(
                    c => c.Name.Equals(
                        request.Name,
                        StringComparison.OrdinalIgnoreCase));

            if (existingCompany != null)
            {
                throw new ConflictException(
                    "Company already exists.");
            }

            var company =
                _mapper.Map<Company>(
                    request);

            await _companyRepository
                .AddAsync(company);
        }

        public async Task<
            IEnumerable<CompanyResponseDto>>
            GetCompaniesAsync()
        {
            var companies =
                await _companyRepository
                    .GetAllAsync();

            companies =
                companies.Where(
                    c => c.IsActive);

            return _mapper.Map<
                IEnumerable<
                    CompanyResponseDto>>(
                        companies);
        }

        public async Task<
            CompanyResponseDto>
            GetCompanyByIdAsync(
                int id)
        {
            var company =
                await _companyRepository
                    .GetByIdAsync(id);

            if (company == null)
            {
                throw new NotFoundException(
                    "Company not found.");
            }

            return _mapper.Map<
                CompanyResponseDto>(
                    company);
        }


        public async Task UpdateCompanyAsync(
            int id,
            UpdateCompanyDto request)
        {
            var company =
                await _companyRepository
                    .GetByIdAsync(id);

            if (company == null)
            {
                throw new NotFoundException(
                    "Company not found.");
            }

            var companies =
                await _companyRepository
                    .GetAllAsync();

            var existingCompany =
                companies.FirstOrDefault(
                    c =>
                        c.Id != id
                        &&
                        c.Name.Equals(
                            request.Name,
                            StringComparison.OrdinalIgnoreCase));

            if (existingCompany != null)
            {
                throw new ConflictException(
                    "Company already exists.");
            }

            _mapper.Map(
                request,
                company);

            await _companyRepository
                .UpdateAsync(company);
        }

        public async Task DeleteCompanyAsync(
            int id)
        {
            var company =
                await _companyRepository
                    .GetByIdAsync(id);

            if (company == null)
            {
                throw new NotFoundException(
                    "Company not found.");
            }

            if (!company.IsActive)
            {
                throw new ConflictException(
                    "Company is already inactive.");
            }

            company.IsActive = false;

            await _companyRepository
                .UpdateAsync(company);
        }
    }
}