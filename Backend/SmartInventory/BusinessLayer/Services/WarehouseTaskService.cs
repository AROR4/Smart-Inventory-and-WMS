using AutoMapper;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.DTOs.Common;
using SmartInventoryManagement.Models.Enums;
using SmartInventoryManagement.Models.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class WarehouseTaskService : IWarehouseTaskService
    {
        private readonly IWarehouseTaskRepository _warehouseTaskRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IInventoryService _inventoryService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IWarehouseTransferService _warehouseTransferService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IWarehouseTransferRepository _transferRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WarehouseTaskService> _logger;
        private readonly IMapper _mapper;

        public WarehouseTaskService(
            IWarehouseTaskRepository warehouseTaskRepository,
            IRepository<Warehouse> warehouseRepository,
            IProductRepository productRepository,
            ICurrentUserService currentUserService,
            IInventoryService inventoryService,
            IPurchaseOrderService purchaseOrderService,
            IWarehouseTransferService warehouseTransferService,
            IPurchaseOrderRepository purchaseOrderRepository,
            IWarehouseTransferRepository transferRepository,
            ApplicationDbContext context,
            ILogger<WarehouseTaskService> logger,
            IMapper mapper)
        {
            _warehouseTaskRepository = warehouseTaskRepository;
            _warehouseRepository = warehouseRepository;
            _productRepository = productRepository;
            _currentUserService = currentUserService;
            _inventoryService = inventoryService;
            _purchaseOrderService = purchaseOrderService;
            _warehouseTransferService = warehouseTransferService;
            _purchaseOrderRepository = purchaseOrderRepository;
            _transferRepository = transferRepository;
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task CreateTaskAsync(
            CreateWarehouseTaskDto request)
        {
            _logger.LogInformation(
                "Creating warehouse task of type {TaskType} for warehouse {WarehouseId}",
                request.Type,
                request.WarehouseId);

            var warehouse = await _warehouseRepository
                    .GetByIdAsync(
                        request.WarehouseId);

            if (warehouse == null ||
                !warehouse.IsActive)
            {
                throw new NotFoundException(
                    "Warehouse not found.");
            }

            if (_currentUserService.AssignedWarehouseId.HasValue &&
                _currentUserService.AssignedWarehouseId.Value !=
                    request.WarehouseId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted unauthorized task creation for warehouse {WarehouseId}",
                    _currentUserService.UserId,
                    request.WarehouseId);
                throw new ForbiddenException(
                    "You can only create tasks for your assigned warehouse.");
            }

            if (request.ReferenceType.HasValue &&
                !request.ReferenceId.HasValue)
            {
                throw new BadRequestException(
                    "Reference Id is required.");
            }

            if (!request.ReferenceType.HasValue &&
                request.ReferenceId.HasValue)
            {
                throw new BadRequestException(
                    "Reference Type is required.");
            }

            var isReferenceTask =
                request.ReferenceType.HasValue &&
                request.ReferenceId.HasValue;
            

            if (!isReferenceTask &&
                !request.Items.Any())
            {
                throw new BadRequestException(
                    "At least one item is required.");
            }

            if (isReferenceTask &&
                request.Items.Any())
            {
                throw new BadRequestException(
                    "Items should not be provided when a reference is specified.");
            }

            var taskItems = new List<WarehouseTaskItem>();

            if (isReferenceTask)
            {
                var existingTask =
                    await _warehouseTaskRepository
                        .GetTaskByReferenceAsync(
                            request.ReferenceType!.Value,
                            request.ReferenceId!.Value,
                            request.WarehouseId,
                            request.Type);

                if (existingTask != null )
                {
                    throw new ConflictException(
                        "A task already exists for this reference.");
                }

                switch (request.ReferenceType)
                {
                    case WarehouseTaskReferenceType.PurchaseOrder:

                        var purchaseOrder =
                            await _purchaseOrderRepository
                                .GetPurchaseOrderWithDetailsAsync(
                                    request.ReferenceId!.Value);

                        if (purchaseOrder == null)
                        {
                            throw new NotFoundException(
                                "Purchase order not found.");
                        }

                        if (purchaseOrder.WarehouseId !=
                            request.WarehouseId)
                        {
                            throw new BadRequestException(
                                "Purchase order does not belong to the selected warehouse.");
                        }

                        if (purchaseOrder.Status !=
                            PurchaseOrderStatus.Received)
                        {
                            throw new BadRequestException(
                                "Only received purchase orders can be processed.");
                        }

                        taskItems =
                            purchaseOrder
                                .PurchaseOrderItems
                                .Select(i =>
                                    new WarehouseTaskItem
                                    {
                                        ProductId =
                                            i.ProductId,

                                        Quantity =
                                            i.OrderedQuantity
                                    })
                                .ToList();

                        break;

                    case WarehouseTaskReferenceType.WarehouseTransfer:

                        var transfer =
                            await _transferRepository
                                .GetTransferWithDetailsAsync(
                                    request.ReferenceId!.Value);

                        if (transfer == null)
                        {
                            throw new NotFoundException(
                                "Transfer not found.");
                        }

                        if (request.WarehouseId != transfer.SourceWarehouseId && request.WarehouseId != transfer.DestinationWarehouseId)
                        {
                            throw new BadRequestException(
                                "Selected warehouse is not part of this transfer.");
                        }

                        if (request.WarehouseId == transfer.SourceWarehouseId)
                        {
                            if (request.Type != WarehouseTaskType.RetrieveInventory)
                            {
                                throw new BadRequestException(
                                    "Only retrieve tasks can be created at the source warehouse.");
                            }
                            if (transfer.Status != TransferStatus.Approved)
                            {
                                throw new BadRequestException(
                                    "Only approved transfers can be processed.");
                            }
                        }

                        if (request.WarehouseId == transfer.DestinationWarehouseId)
                        {
                            if (request.Type != WarehouseTaskType.StoreInventory)
                            {
                                throw new BadRequestException(
                                    "Only store tasks can be created at the destination warehouse.");
                            }
                            if (transfer.Status != TransferStatus.Received)
                            {
                                throw new BadRequestException(
                                    "Only received transfers can be processed.");
                            }
                        }

                        taskItems =
                            transfer.WarehouseTransferItems
                                .Select(i =>
                                    new WarehouseTaskItem
                                    {
                                        ProductId =
                                            i.ProductId,

                                        Quantity =
                                            i.Quantity
                                    })
                                .ToList();


                        break;

                    case WarehouseTaskReferenceType.DispatchOrder:

                        throw new BadRequestException(
                            "Dispatch orders are not implemented yet.");

                    default:

                        throw new BadRequestException(
                            "Invalid reference type.");
                }
            }
            else
            {
                var duplicateProducts =
                    request.Items
                        .GroupBy(
                            i => i.ProductId)
                        .Any(
                            g => g.Count() > 1);

                if (duplicateProducts)
                {
                    throw new BadRequestException(
                        "Duplicate products are not allowed.");
                }

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new BadRequestException(
                            "Quantity must be greater than zero.");
                    }

                    var product =
                        await _productRepository
                            .GetByIdAsync(
                                item.ProductId);

                    if (product == null ||
                        !product.IsActive)
                    {
                        throw new NotFoundException(
                            $"Product {item.ProductId} not found.");
                    }
                }
            }

            var task =
                new WarehouseTask
                {
                    Type =
                        request.Type,

                    WarehouseId =
                        request.WarehouseId,

                    Description =
                        request.Description,

                    Status =
                        TaskStatusType.Pending,

                    CreatedByUserId =
                        _currentUserService.UserId,

                    ReferenceType =
                        request.ReferenceType,

                    ReferenceId =
                        request.ReferenceId,

                    WarehouseTaskItems =
                        isReferenceTask
                            ? taskItems
                            : request.Items
                                .Select(
                                    i => new WarehouseTaskItem
                                    {
                                        ProductId =
                                            i.ProductId,

                                        Quantity =
                                            i.Quantity
                                    })
                                .ToList()
                };

            await _warehouseTaskRepository
                .AddAsync(task);

            _logger.LogInformation(
                "Warehouse task {TaskId} created successfully",
                task.Id);
        }

        public async Task<WarehouseTaskResponseDto>GetTaskByIdAsync(int taskId)
        {
            var task =await _warehouseTaskRepository.GetTaskWithDetailsAsync(taskId);

            if (task == null)
            {
                throw new NotFoundException(
                    "Task not found.");
            }

            if (_currentUserService.Role == "WarehouseManager" && _currentUserService.AssignedWarehouseId != task.WarehouseId)
            {
                throw new ForbiddenException(
                    "You are not authorized to access this task.");
            }

            return _mapper.Map<WarehouseTaskResponseDto>(task);
        }

        public async Task<PagedResponseDto<WarehouseTaskResponseDto>>GetTasksAsync(
                PaginationParams pagination,
                WarehouseTaskFilterDto filter)
        {
            var tasks =
                await _warehouseTaskRepository
                    .GetTasksWithDetailsAsync();

            if (_currentUserService.Role == "WarehouseManager" || _currentUserService.Role == "InventoryStaff")
            {
                tasks = tasks.Where(
                    t => t.WarehouseId ==
                         _currentUserService.AssignedWarehouseId);
            }

            

            if (filter.Type.HasValue)
            {
                tasks = tasks.Where(
                    t => t.Type ==
                        filter.Type.Value);
            }

            if (filter.Status.HasValue)
            {
                tasks = tasks.Where(
                    t => t.Status ==
                        filter.Status.Value);
            }

            if (filter.WarehouseId.HasValue)
            {
                tasks = tasks.Where(
                    t => t.WarehouseId ==
                        filter.WarehouseId.Value);
            }

            var totalRecords =
                tasks.Count();

            var pagedTasks =
                tasks.Skip(
                        (pagination.PageNumber - 1)
                        * pagination.PageSize)
                    .Take(
                        pagination.PageSize);

            return new PagedResponseDto<
                WarehouseTaskResponseDto>
            {
                Data =
                    _mapper.Map<
                        IEnumerable<
                            WarehouseTaskResponseDto>>(
                                pagedTasks),

                PageNumber =
                    pagination.PageNumber,

                PageSize =
                    pagination.PageSize,

                TotalRecords =
                    totalRecords,

                TotalPages =
                    (int)Math.Ceiling(
                        totalRecords /
                        (double)pagination.PageSize)
            };
        }


        public async Task StartTaskAsync(
            int taskId)
        {
            _logger.LogInformation(
                "Starting warehouse task {TaskId} by user {UserId}",
                taskId,
                _currentUserService.UserId);

            var task =
                await _warehouseTaskRepository
                    .GetByIdAsync(taskId);

            if (task == null)
            {
                throw new NotFoundException(
                    "Task not found.");
            }

            if (task.Status !=
                TaskStatusType.Pending)
            {
                throw new BadRequestException(
                    "Only pending tasks can be started.");
            }

            task.Status =
                TaskStatusType.InProgress;

            task.StartedByUserId =
                _currentUserService.UserId;

            task.StartedAt =
                DateTime.Now;

            await _warehouseTaskRepository
                .UpdateAsync(task);

            _logger.LogInformation(
                "Warehouse task {TaskId} started successfully",
                taskId);
        }

        public async Task CompleteTaskAsync(
            int taskId)
        {
            _logger.LogInformation(
                "Completing warehouse task {TaskId} by user {UserId}",
                taskId,
                _currentUserService.UserId);

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var task =
                    await _warehouseTaskRepository
                        .GetTaskWithDetailsAsync(taskId);

                if (task == null)
                {
                    throw new NotFoundException(
                        "Task not found.");
                }

                if (_currentUserService.Role == "WarehouseManager" && _currentUserService.AssignedWarehouseId != task.WarehouseId)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted unauthorized completion of task {TaskId}",
                        _currentUserService.UserId,
                        taskId);
                    throw new ForbiddenException(
                        "You are not authorized to complete this task.");
                }

                if (task.Status !=
                    TaskStatusType.InProgress)
                {
                    _logger.LogWarning(
                        "Attempted completion of task {TaskId} which is in status {Status}",
                        taskId,
                        task.Status);
                    throw new BadRequestException(
                        "Only tasks in progress can be completed.");
                }

                if (task.StartedByUserId !=
                    _currentUserService.UserId)
                {
                    throw new BadRequestException(
                        "Only the user who started the task can complete it.");
                }

                foreach (var item in task.WarehouseTaskItems)
                {
                    if (task.Type ==
                        WarehouseTaskType.StoreInventory)
                    {
                        await _inventoryService
                            .ExecuteStoreInventoryAsync(
                                item.ProductId,
                                task.WarehouseId,
                                item.Quantity,
                                task.Description);
                    }
                    else
                    {
                        await _inventoryService
                            .ExecuteRetrieveInventoryAsync(
                                item.ProductId,
                                task.WarehouseId,
                                item.Quantity,
                                task.Description);
                    }
                }

                if (task.ReferenceType.HasValue &&
                    task.ReferenceId.HasValue)
                {
                    
                    switch (task.ReferenceType)
                    {
                        case WarehouseTaskReferenceType.PurchaseOrder:

                            if (task.Type ==
                                WarehouseTaskType.StoreInventory)
                            {
                                await _purchaseOrderService
                                    .CompletePurchaseOrderAsync(
                                        task.ReferenceId!.Value);
                            }

                            break;

                        case WarehouseTaskReferenceType.WarehouseTransfer:

                            if (task.Type ==
                                WarehouseTaskType.RetrieveInventory)
                            {
                                await _warehouseTransferService
                                    .MarkInTransitAsync(
                                        task.ReferenceId!.Value);
                            }

                            else if (task.Type ==
                                    WarehouseTaskType.StoreInventory)
                            {
                                await _warehouseTransferService
                                    .CompleteTransferAsync(
                                        task.ReferenceId!.Value);
                            }

                            break;

                        case WarehouseTaskReferenceType.DispatchOrder:

                            // Future implementation

                            break;
                    }
                }

                task.Status =TaskStatusType.Completed;

                task.CompletedByUserId =_currentUserService.UserId;

                task.CompletedAt =DateTime.Now;

                await _warehouseTaskRepository.UpdateAsync(task);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Warehouse task {TaskId} completed successfully",
                    taskId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



    }
}