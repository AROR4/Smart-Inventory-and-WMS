namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; }

        string Email { get; }

        string Role { get; }

        int? AssignedWarehouseId { get; }

        int ? SupplierId { get; }
    }
}