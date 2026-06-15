using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventory.Tests;

public class CurrentUserServiceTests
{

   private CurrentUserService _currentUserService = null!;

    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;

    [SetUp]
    public void Setup()
    {
        _httpContextAccessor =
            new Mock<IHttpContextAccessor>();

        _currentUserService =
            new CurrentUserService(
                _httpContextAccessor.Object);
    }

    private void SetUserClaims(
        params Claim[] claims)
    {
        var identity =
            new ClaimsIdentity(
                claims,
                "TestAuth");

        var principal =
            new ClaimsPrincipal(
                identity);

        var context =
            new DefaultHttpContext
            {
                User = principal
            };

        _httpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(context);
    }

    #region User Claim Tests

    [Test]
    public void UserId_ReturnsValue()
    {
        SetUserClaims(
            new Claim(
                ClaimTypes.NameIdentifier,
                "10"));

        Assert.That(
            _currentUserService.UserId,
            Is.EqualTo(10));
    }

    [Test]
    public void Email_ReturnsValue()
    {
        SetUserClaims(
            new Claim(
                ClaimTypes.Email,
                "raghav@test.com"));

        Assert.That(
            _currentUserService.Email,
            Is.EqualTo(
                "raghav@test.com"));
    }
    
    [Test]
    public void Role_ReturnsValue()
    {
        SetUserClaims(
            new Claim(
                ClaimTypes.Role,
                "Admin"));

        Assert.That(
            _currentUserService.Role,
            Is.EqualTo(
                "Admin"));
    }

    [Test]
    public void AssignedWarehouseId_ReturnsValue()
    {
        SetUserClaims(
            new Claim(
                "AssignedWarehouseId",
                "5"));

        Assert.That(
            _currentUserService
                .AssignedWarehouseId,
            Is.EqualTo(5));
    }

    [Test]
    public void AssignedWarehouseId_ReturnsNull()
    {
        SetUserClaims();

        Assert.That(
            _currentUserService
                .AssignedWarehouseId,
            Is.Null);
    }

    [Test]
    public void SupplierId_ReturnsValue()
    {
        SetUserClaims(
            new Claim(
                "SupplierId",
                "7"));

        Assert.That(
            _currentUserService
                .SupplierId,
            Is.EqualTo(7));
    }

    [Test]
    public void SupplierId_ReturnsNull()
    {
        SetUserClaims();

        Assert.That(
            _currentUserService
                .SupplierId,
            Is.Null);
    }

    [Test]
    public void UserId_NoClaim_Throws()
    {
        SetUserClaims();

        Assert.Throws<ArgumentNullException>(
            () =>
            {
                var id =
                    _currentUserService
                        .UserId;
            });
    }

    #endregion
}
