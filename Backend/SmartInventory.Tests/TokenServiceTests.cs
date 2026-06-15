using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Models.DTOs;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartInventory.Tests;

[TestFixture]
public class TokenServiceTests
{
    private const string SecretKey = "ThisIsMySuperSecretKeyForTesting123456";
    private const string Issuer = "SmartInventoryManagement";
    private const string Audience = "SmartInventoryUsers";
    private const string DurationInMinutes = "60";

    #region Constructor Tests

    [Test]
    public void Constructor_MissingKey_ThrowsException()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var ex = Assert.Throws<Exception>(() => new TokenService(config));
        Assert.That(ex.Message, Is.EqualTo("JWT Key is missing."));
    }

    [Test]
    public void Constructor_MissingIssuer_ThrowsException()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var ex = Assert.Throws<Exception>(() => new TokenService(config));
        Assert.That(ex.Message, Is.EqualTo("JWT Issuer is missing."));
    }

    [Test]
    public void Constructor_MissingAudience_ThrowsException()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var ex = Assert.Throws<Exception>(() => new TokenService(config));
        Assert.That(ex.Message, Is.EqualTo("JWT Audience is missing."));
    }

    #endregion

    #region GenerateToken Tests

    [Test]
    public void GenerateToken_ValidRequest_GeneratesTokenWithCorrectClaims()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var tokenService = new TokenService(config);

        var request = new TokenRequest
        {
            Id = 123,
            Name = "John Doe",
            Email = "john@example.com",
            Role = "Admin",
            AssignedWarehouseId = 45,
            SupplierId = 67
        };

        var token = tokenService.GenerateToken(request);
        Assert.That(token, Is.Not.Null.And.Not.Empty);

        // Validate token claims
        var principal = tokenService.ValidateToken(token);
        Assert.That(principal, Is.Not.Null);

        var claimsIdentity = principal.Identity as ClaimsIdentity;
        Assert.That(claimsIdentity, Is.Not.Null);

        var nameIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        Assert.That(nameIdClaim?.Value, Is.EqualTo("123"));

        var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);
        Assert.That(emailClaim?.Value, Is.EqualTo("john@example.com"));

        var nameClaim = claimsIdentity.FindFirst(ClaimTypes.Name);
        Assert.That(nameClaim?.Value, Is.EqualTo("John Doe"));

        var roleClaim = claimsIdentity.FindFirst(ClaimTypes.Role);
        Assert.That(roleClaim?.Value, Is.EqualTo("Admin"));

        var warehouseClaim = claimsIdentity.FindFirst("AssignedWarehouseId");
        Assert.That(warehouseClaim?.Value, Is.EqualTo("45"));

        var supplierClaim = claimsIdentity.FindFirst("SupplierId");
        Assert.That(supplierClaim?.Value, Is.EqualTo("67"));
    }

    [Test]
    public void GenerateToken_MissingOptionalProperties_GeneratesTokenWithoutOptionalClaims()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var tokenService = new TokenService(config);

        var request = new TokenRequest
        {
            Id = 456,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = "User",
            AssignedWarehouseId = null,
            SupplierId = null
        };

        var token = tokenService.GenerateToken(request);
        Assert.That(token, Is.Not.Null.And.Not.Empty);

        var principal = tokenService.ValidateToken(token);
        var claimsIdentity = principal.Identity as ClaimsIdentity;

        Assert.That(claimsIdentity!.FindFirst("AssignedWarehouseId"), Is.Null);
        Assert.That(claimsIdentity.FindFirst("SupplierId"), Is.Null);
    }

    #endregion

    #region GeneratePasswordSetupToken Tests

    [Test]
    public void GeneratePasswordSetupToken_ValidUserId_GeneratesPasswordSetupToken()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var tokenService = new TokenService(config);

        var token = tokenService.GeneratePasswordSetupToken(789);
        Assert.That(token, Is.Not.Null.And.Not.Empty);

        var principal = tokenService.ValidateToken(token);
        var claimsIdentity = principal.Identity as ClaimsIdentity;

        var subClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        Assert.That(subClaim?.Value, Is.EqualTo("789"));

        var purposeClaim = claimsIdentity?.FindFirst("Purpose");
        Assert.That(purposeClaim?.Value, Is.EqualTo("PasswordSetup"));
    }

    #endregion

    #region ValidateToken Tests

    [Test]
    public void ValidateToken_InvalidSignature_ThrowsException()
    {
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", DurationInMinutes}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var tokenService = new TokenService(config);

        var request = new TokenRequest { Id = 1, Name = "A", Email = "a@a.com", Role = "R" };
        var token = tokenService.GenerateToken(request);

        // Tamper with the token (alter the signature part)
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.{parts[1]}.InvalidSignatureStuff";

        Assert.Throws<SecurityTokenInvalidSignatureException>(() => tokenService.ValidateToken(tamperedToken));
    }

    [Test]
    public void ValidateToken_ExpiredToken_ThrowsException()
    {
        // Set duration to -10 minutes to exceed default clock skew of 5 minutes
        var settings = new Dictionary<string, string?>
        {
            {"JWT:Key", SecretKey},
            {"JWT:Issuer", Issuer},
            {"JWT:Audience", Audience},
            {"JWT:DurationInMinutes", "-10"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var tokenService = new TokenService(config);

        var request = new TokenRequest { Id = 1, Name = "A", Email = "a@a.com", Role = "R" };
        var token = tokenService.GenerateToken(request);

        Assert.Throws<SecurityTokenExpiredException>(() => tokenService.ValidateToken(token));
    }

    #endregion
}
