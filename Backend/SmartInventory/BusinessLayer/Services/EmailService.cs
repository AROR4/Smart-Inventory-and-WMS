using Microsoft.Extensions.Options;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.Configurations;
using System.Net;
using System.Net.Mail;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(
            IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            using var smtpClient =
                new SmtpClient(
                    _settings.Host,
                    _settings.Port);

            smtpClient.Credentials =
                new NetworkCredential(
                    _settings.Email,
                    _settings.Password);

            smtpClient.EnableSsl = true;

            var mailMessage =
                new MailMessage
                {
                    From = new MailAddress(
                        _settings.Email),

                    Subject = subject,

                    Body = body,

                    IsBodyHtml = true
                };

            mailMessage.To.Add(to);

            await smtpClient
                .SendMailAsync(mailMessage);
        }

        public async Task SendPasswordSetupEmailAsync(
            string email,
            string name,
            string role,
            string setupLink)
        {
            var subject = "Welcome to Smart Inventory Management";

            var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to Smart Inventory Management</h2>

                <p>Hello {name},</p>

                <p>
                    You have been added to the Smart Inventory Management System
                    as a <strong>{role}</strong>.
                </p>

                <p>
                    To activate your account and create your password,
                    please click the button below:
                </p>

                <p>
                    <a href='{setupLink}'
                    style='background-color:#2563eb;
                            color:white;
                            padding:12px 20px;
                            text-decoration:none;
                            border-radius:5px;'>
                        Set Up Account
                    </a>
                </p>

                <p>
                    This link will expire in 24 hours.
                </p>

                <p>
                    If you did not expect this invitation,
                    please ignore this email.
                </p>

                <br/>

                <p>
                    Regards,<br/>
                    Smart Inventory Management Team
                </p>
            </body>
            </html>";

            await SendEmailAsync(
                email,
                subject,
                body);
        }
        
    }
}