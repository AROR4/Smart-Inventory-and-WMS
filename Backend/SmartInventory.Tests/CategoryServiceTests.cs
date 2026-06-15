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

public class CategoryServiceTests
{

    private IRepository<Category> _categoryRepository = null!;

    private CategoryService _categoryService = null!;

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


        _categoryRepository = new Repository<Category>(_context);

            _categoryService =new CategoryService(
            _categoryRepository,
            _mapper);
    }

    #region CreateCategory Tests

    [Test]
    public async Task CreateCategory_Success()
    {
        var request = new CreateCategoryDto
        {
            Name = "Electronics"
        };

        await _categoryService
            .CreateCategoryAsync(request);

        var category =
            (await _categoryRepository
                .FindAsync(
                    c => c.Name == "Electronics"))
            .FirstOrDefault();

        Assert.That(
            category,
            Is.Not.Null);
    }

    [Test]
    public async Task CreateCategory_Duplicate()
    {
        await _categoryRepository
            .AddAsync(
                new Category
                {
                    Name = "Electronics",
                    IsActive = true
                });

        var request = new CreateCategoryDto
        {
            Name = "Electronics"
        };

        Assert.ThrowsAsync<
            ConflictException>(
            async () =>
                await _categoryService
                    .CreateCategoryAsync(
                        request));
    }

    #endregion

    #region UpdateCategory Tests

    [Test]
    public void UpdateCategory_NotFound()
    {
        var request =
            new UpdateCategoryDto
            {
                Name = "Updated"
            };

        Assert.ThrowsAsync<
            NotFoundException>(
            async () =>
                await _categoryService
                    .UpdateCategoryAsync(
                        999,
                        request));
    }

    [Test]
    public async Task UpdateCategory_Success()
    {
        var category =
            new Category
            {
                Name = "Old Name",
                IsActive = true
            };

        await _categoryRepository
            .AddAsync(category);

        await _categoryService
            .UpdateCategoryAsync(
                category.Id,
                new UpdateCategoryDto
                {
                    Name = "New Name"
                });

        var updated =
            await _categoryRepository
                .GetByIdAsync(
                    category.Id);

        Assert.That(
            updated!.Name,
            Is.EqualTo(
                "New Name"));
    }

    #endregion

    #region DeleteCategory Tests

    [Test]
    public void DeleteCategory_NotFound()
    {
        Assert.ThrowsAsync<
            NotFoundException>(
            async () =>
                await _categoryService
                    .DeleteCategoryAsync(
                        999));
    }

    [Test]
    public async Task DeleteCategory_Success()
    {
        var category =
            new Category
            {
                Name = "Electronics",
                IsActive = true
            };

        await _categoryRepository
            .AddAsync(category);

        await _categoryService
            .DeleteCategoryAsync(
                category.Id);

        var deleted =
            await _categoryRepository
                .GetByIdAsync(
                    category.Id);

        Assert.That(
            deleted!.IsActive,
            Is.False);
    }

    #endregion

    #region GetCategoryById Tests

    [Test]
    public void GetCategoryById_NotFound()
    {
        Assert.ThrowsAsync<
            NotFoundException>(
            async () =>
                await _categoryService
                    .GetCategoryByIdAsync(
                        999));
    }

    [Test]
    public async Task GetCategoryById_Success()
    {
        var category =
            new Category
            {
                Name = "Electronics",
                IsActive = true
            };

        await _categoryRepository
            .AddAsync(category);

        var result =
            await _categoryService
                .GetCategoryByIdAsync(
                    category.Id);

        Assert.That(
            result.Name,
            Is.EqualTo(
                "Electronics"));
    }

    #endregion

    #region GetCategories Tests

    [Test]
    public async Task GetCategories_ReturnsOnlyActive()
    {
        await _categoryRepository
            .AddAsync(
                new Category
                {
                    Name = "Active",
                    IsActive = true
                });

        await _categoryRepository
            .AddAsync(
                new Category
                {
                    Name = "Inactive",
                    IsActive = false
                });

        var result =
            await _categoryService
                .GetCategoriesAsync();

        Assert.That(
            result.Count(),
            Is.EqualTo(1));

        Assert.That(
            result.First().Name,
            Is.EqualTo(
                "Active"));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

   
}
