using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Interfaces
{
    public interface IWarehouseService
    {
        Task CreateWarehouseAsync(CreateWarehouseDto request);

        Task UpdateWarehouseAsync(
            int id,
            UpdateWarehouseDto request);

        Task DeleteWarehouseAsync(int id);

        Task<WarehouseResponseDto> GetWarehouseByIdAsync(int id);

        Task<IEnumerable<WarehouseResponseDto>> GetAllWarehousesAsync();
    }
}