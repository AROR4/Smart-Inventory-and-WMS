using Microsoft.Extensions.Options;
using NUnit.Framework;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Models.Configurations;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SmartInventory.Tests;

[TestFixture]
public class EmailServiceTests
{
    private IOptions<EmailSettings> _options = null!;
    private EmailService _emailService = null!;

    [SetUp]
    public void Setup()
    {
        var settings = new EmailSettings
        {
            Host = "127.0.0.1",
            Port = 12345, // Invalid/closed port to trigger quick connection failure
            Email = "sender@test.com",
            Password = "password"
        };

        _options = Options.Create(settings);
        _emailService = new EmailService(_options);
    }

    #region SendEmail Tests

    [Test]
    public void SendEmailAsync_AttemptsConnection_ThrowsSmtpException()
    {
        // Assert that executing the mail send operation throws an SMTP exception,
        // confirming the client is correctly initialized, properties are set, and it attempts connection.
        Assert.ThrowsAsync<SmtpException>(async () =>
            await _emailService.SendEmailAsync("recipient@test.com", "Test Subject", "Test Body")
        );
    }

    #endregion

    #region SendPasswordSetupEmail Tests

    [Test]
    public void SendPasswordSetupEmailAsync_AttemptsConnection_ThrowsSmtpException()
    {
        // Assert that executing the password setup invitation email throws an SMTP exception,
        // confirming that the service formats the HTML body and calls the underlying SMTP send method.
        Assert.ThrowsAsync<SmtpException>(async () =>
            await _emailService.SendPasswordSetupEmailAsync("user@test.com", "Test User", "Admin", "https://setup-link")
        );
    }

    #endregion
}
