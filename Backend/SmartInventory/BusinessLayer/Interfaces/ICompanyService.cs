namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ICompanyService
    {
        Task CreateCompanyAsync(
            CreateCompanyDto request);

        Task UpdateCompanyAsync(
            int id,
            UpdateCompanyDto request);

        Task DeleteCompanyAsync(
            int id);

        Task<
            IEnumerable<
                CompanyResponseDto>>
            GetCompaniesAsync();

        Task<
            CompanyResponseDto>
            GetCompanyByIdAsync(
                int id);
    }
}