namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string body);

        Task SendPasswordSetupEmailAsync(
            string email,
            string name,
            string role,
            string setupLink);
    }
}