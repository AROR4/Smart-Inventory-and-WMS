using AutoMapper;
using SmartInventoryManagement.Models.Exceptions;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly ICurrentUserService _currentUserService;

        private readonly ILogger<WarehouseService> _logger;
        private readonly IMapper _mapper;

        public WarehouseService(
            IRepository<Warehouse> warehouseRepository,
            ICurrentUserService currentUserService,
            ILogger<WarehouseService> logger,
            IMapper mapper)
        {
            _warehouseRepository = warehouseRepository;
            _currentUserService = currentUserService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task CreateWarehouseAsync(
            CreateWarehouseDto request)
        {
            var existing =(await _warehouseRepository.GetAllAsync()).FirstOrDefault(w => w.Name == request.Name);

            if(existing != null)
            {
                _logger.LogWarning(
                    "Attempt to create duplicate warehouse with name {WarehouseName}",
                    request.Name);
                throw new ConflictException(
                    "Warehouse already exists.");
            }
            var warehouse =_mapper.Map<Warehouse>(request);

            warehouse.AvailableCapacity = request.Capacity;

            await _warehouseRepository.AddAsync(warehouse);
            _logger.LogInformation(
                "Warehouse {WarehouseName} created with ID {WarehouseId}",
                warehouse.Name,
                warehouse.Id);
        }

        public async Task UpdateWarehouseAsync(int id,UpdateWarehouseDto request)
        {
            var warehouse =await _warehouseRepository.GetByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Warehouse not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" &&
                warehouse.Id != _currentUserService.AssignedWarehouseId)
            {
                throw new ForbiddenException("You can only access your assigned warehouse.");
            }

            var oldCapacity = warehouse.Capacity;

            _mapper.Map(
                request,
                warehouse);

            var capacityDifference =
                warehouse.Capacity -
                oldCapacity;

            warehouse.AvailableCapacity +=
                capacityDifference;

            if (warehouse.AvailableCapacity < 0)
            {
                _logger.LogWarning(
                    "Attempt to reduce capacity of warehouse with ID {WarehouseId} below reserved capacity",
                    warehouse.Id);
                throw new ConflictException(
                    "Warehouse capacity cannot be less than reserved capacity.");
            }

            await _warehouseRepository.UpdateAsync(warehouse);
            _logger.LogInformation(
                "Warehouse with ID {WarehouseId} updated",
                warehouse.Id);
        }

        public async Task DeleteWarehouseAsync(int id)
        {
            var warehouse =await _warehouseRepository.GetByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Warehouse not found.");
            }

            if (warehouse.Capacity - warehouse.AvailableCapacity > 0)
            {
                _logger.LogWarning(
                    "Attempt to delete warehouse with ID {WarehouseId} that has inventory",
                    warehouse.Id);
                
                throw new ConflictException(
                    "Cannot delete warehouse with inventory.");
            }

            warehouse.IsActive = false;

            await _warehouseRepository.UpdateAsync(warehouse);
            _logger.LogInformation(
                "Warehouse with ID {WarehouseId} marked as inactive",
                warehouse.Id);
        }

        public async Task<WarehouseResponseDto>GetWarehouseByIdAsync(int id)
        {
            var warehouse =await _warehouseRepository.GetByIdAsync(id);

            if (warehouse == null)
            {
                throw new NotFoundException("Warehouse not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" &&
                warehouse.Id != _currentUserService.AssignedWarehouseId)
            {
                throw new ForbiddenException("You can only access your assigned warehouse.");
            }

            return _mapper.Map<WarehouseResponseDto>(warehouse);
        }


        public async Task<IEnumerable<WarehouseResponseDto>>
            GetAllWarehousesAsync()
        {
            var warehouses =await _warehouseRepository.GetAllAsync();

            if(_currentUserService.Role== "WarehouseManager")
            {
                warehouses = warehouses.Where(w => w.Id == _currentUserService.AssignedWarehouseId);
            }

            return _mapper.Map<IEnumerable<WarehouseResponseDto>>(warehouses);
        }
    }
}