using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventory.Tests;

public class CompanyServiceTests
{

   private IRepository<Company> _companyRepository = null!;

    private CompanyService _companyService = null!;

    private ApplicationDbContext _context = null!;

    private IMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        _context =new ApplicationDbContext(options);
        
        var mapperConfig =
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

        _mapper =
            mapperConfig.CreateMapper();


        _companyRepository = new Repository<Company>(_context);

            _companyService =new CompanyService(
            _companyRepository,
            _mapper);
    }

    #region CreateCompany Tests

    [Test]
    public async Task CreateCompany_Success()
    {
        await _companyService.CreateCompanyAsync(
            new CreateCompanyDto
            {
                Name = "Dell"
            });

        var company =
            (await _companyRepository.GetAllAsync())
            .FirstOrDefault(
                c => c.Name == "Dell");

        Assert.That(company, Is.Not.Null);
    }

    [Test]
    public async Task CreateCompany_Duplicate()
    {
        await _companyRepository.AddAsync(
            new Company
            {
                Name = "Dell",
                IsActive = true
            });

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _companyService.CreateCompanyAsync(
                    new CreateCompanyDto
                    {
                        Name = "Dell"
                    }));
    }

    [Test]
    public async Task CreateCompany_DuplicateIgnoreCase()
    {
        await _companyRepository.AddAsync(
            new Company
            {
                Name = "Dell",
                IsActive = true
            });

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _companyService.CreateCompanyAsync(
                    new CreateCompanyDto
                    {
                        Name = "dell"
                    }));
    }

    #endregion

    #region GetCompanies Tests

    [Test]
    public async Task GetCompanies_ReturnsOnlyActive()
    {
        await _companyRepository.AddAsync(
            new Company
            {
                Name = "Dell",
                IsActive = true
            });

        await _companyRepository.AddAsync(
            new Company
            {
                Name = "HP",
                IsActive = false
            });

        var result =
            await _companyService
                .GetCompaniesAsync();

        Assert.That(
            result.Count(),
            Is.EqualTo(1));
    }

    [Test]
    public async Task GetCompanies_Empty()
    {
        var result =
            await _companyService
                .GetCompaniesAsync();

        Assert.That(
            result,
            Is.Empty);
    }

    #endregion

    #region GetCompanyById Tests

    [Test]
    public void GetCompanyById_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _companyService
                    .GetCompanyByIdAsync(999));
    }

    [Test]
    public async Task GetCompanyById_Success()
    {
        var company = new Company
        {
            Name = "Dell",
            IsActive = true
        };

        await _companyRepository
            .AddAsync(company);

        var result =
            await _companyService
                .GetCompanyByIdAsync(
                    company.Id);

        Assert.That(
            result.Name,
            Is.EqualTo("Dell"));
    }

    #endregion

    #region UpdateCompany Tests

    [Test]
    public void UpdateCompany_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _companyService
                    .UpdateCompanyAsync(
                        999,
                        new UpdateCompanyDto
                        {
                            Name = "Updated"
                        }));
    }

    [Test]
    public async Task UpdateCompany_Duplicate()
    {
        var company1 = new Company
        {
            Name = "Dell",
            IsActive = true
        };

        var company2 = new Company
        {
            Name = "HP",
            IsActive = true
        };

        await _companyRepository.AddAsync(company1);
        await _companyRepository.AddAsync(company2);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _companyService
                    .UpdateCompanyAsync(
                        company2.Id,
                        new UpdateCompanyDto
                        {
                            Name = "Dell"
                        }));
    }

    [Test]
    public async Task UpdateCompany_Success()
    {
        var company = new Company
        {
            Name = "Dell",
            IsActive = true
        };

        await _companyRepository.AddAsync(company);

        await _companyService
            .UpdateCompanyAsync(
                company.Id,
                new UpdateCompanyDto
                {
                    Name = "Dell Technologies"
                });

        var updated =
            await _companyRepository
                .GetByIdAsync(
                    company.Id);

        Assert.That(
            updated!.Name,
            Is.EqualTo(
                "Dell Technologies"));
    }


    #endregion

    #region DeleteCompany Tests

    [Test]
    public async Task DeleteCompany_AlreadyInactive()
    {
        var company = new Company
        {
            Name = "Dell",
            IsActive = false
        };

        await _companyRepository.AddAsync(company);

        Assert.ThrowsAsync<ConflictException>(
            async () =>
                await _companyService
                    .DeleteCompanyAsync(
                        company.Id));
    }

    [Test]
    public async Task DeleteCompany_Success()
    {
        var company = new Company
        {
            Name = "Dell",
            IsActive = true
        };

        await _companyRepository.AddAsync(company);

        await _companyService.DeleteCompanyAsync(company.Id);

        var updatedCompany =
            await _companyRepository.GetByIdAsync(company.Id);

        Assert.That(updatedCompany, Is.Not.Null);
        Assert.That(updatedCompany!.IsActive, Is.False);
    }

    [Test]
    public void DeleteCompany_NotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(
            async () =>
                await _companyService.DeleteCompanyAsync(999));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

   
}
