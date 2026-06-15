using AutoMapper;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.DTOs;

namespace SmartInventoryManagement.BusinessLayer.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateUserDto, User>();

            CreateMap<User, UserResponseDto>()
                .ForMember(
                    dest => dest.Role,
                    opt => opt.MapFrom(
                        src => src.Role.Name))

                .ForMember(
                    dest => dest.SupplierCompanyName,
                    opt => opt.MapFrom(
                        src => src.Supplier != null
                            ? src.Supplier.Name
                            : null))

                .ForMember(
                    dest => dest.SupplierEmail,
                    opt => opt.MapFrom(
                        src => src.Supplier != null
                            ? src.Supplier.Email
                            : null))

                .ForMember(
                    dest => dest.SupplierPhone,
                    opt => opt.MapFrom(
                        src => src.Supplier != null
                            ? src.Supplier.PhoneNumber
                            : null))

                .ForMember(
                    dest => dest.SupplierAddress,
                    opt => opt.MapFrom(
                        src => src.Supplier != null
                            ? src.Supplier.Address
                            : null));

            CreateMap<CreateWarehouseDto, Warehouse>();

            CreateMap<UpdateWarehouseDto, Warehouse>()
                .ForAllMembers(opts =>
                    opts.Condition(
                        (src, dest, srcMember) =>
                            srcMember != null));

            CreateMap<Warehouse, WarehouseResponseDto>()
                .ForMember(
                dest => dest.EffectiveCapacity,
                opt => opt.MapFrom(
                    src => src.AvailableCapacity -
                        src.ReservedCapacity));


            CreateMap<CreateCategoryDto, Category>();

            CreateMap<Category, CategoryResponseDto>();
            
            CreateMap<CreateProductDto, Product>();

            CreateMap<Product, ProductResponseDto>()
                .ForMember(
                    dest => dest.Category,
                    opt => opt.MapFrom(
                        src => src.Category.Name))
                .ForMember(
                    dest => dest.RequiredStorageType,
                    opt => opt.MapFrom(
                        src => src.RequiredStorageType.ToString()))
                .ForMember(
                    dest => dest.Volume,
                    opt => opt.MapFrom(
                        src => src.Length *
                            src.Width *
                            src.Height))
                .ForMember(
                    dest => dest.Company,
                    opt => opt.MapFrom(
                        src => src.Company.Name));


            CreateMap<Inventory,InventoryResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                        src => src.Product.Name))
                .ForMember(
                    dest => dest.CompanyName,
                    opt => opt.MapFrom(
                        src => src.Product.Company.Name))
                .ForMember(
                    dest => dest.WarehouseName,
                    opt => opt.MapFrom(
                        src => src.Warehouse.Name))
                .ForMember(
                    dest => dest.OccupiedVolume,
                    opt => opt.MapFrom(
                        src =>
                            src.Quantity *
                            src.Product.Length *
                            src.Product.Width *
                            src.Product.Height));

            CreateMap<StockMovement, StockMovementResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                        src => src.Product.Name))
                .ForMember(
                    dest => dest.WarehouseName,
                    opt => opt.MapFrom(
                        src => src.Warehouse.Name))
                .ForMember(
                    dest => dest.PerformedBy,
                    opt => opt.MapFrom(
                        src => src.CreatedByUser.Name))
                .ForMember(
                    dest => dest.Type,
                    opt => opt.MapFrom(
                        src => src.Type.ToString()));

            CreateMap<WarehouseTaskItem,WarehouseTaskItemResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                        src => src.Product.Name));

            CreateMap<
                WarehouseTask,
                WarehouseTaskResponseDto>()
                .ForMember(
                    dest => dest.Type,
                    opt => opt.MapFrom(
                        src => src.Type.ToString()))
                .ForMember(
                    dest => dest.Status,
                    opt => opt.MapFrom(
                        src => src.Status.ToString()))
                .ForMember(
                    dest => dest.WarehouseName,
                    opt => opt.MapFrom(
                        src => src.Warehouse.Name))
                .ForMember(
                    dest => dest.CreatedBy,
                    opt => opt.MapFrom(
                        src => src.CreatedByUser.Name))
                .ForMember(
                    dest => dest.StartedBy,
                    opt => opt.MapFrom(
                        src => src.StartedByUser != null
                            ? src.StartedByUser.Name
                            : null))
                .ForMember(
                    dest => dest.CompletedBy,
                    opt => opt.MapFrom(
                        src => src.CompletedByUser != null
                            ? src.CompletedByUser.Name
                            : null))
                .ForMember(
                    dest => dest.ReferenceType,
                    opt => opt.MapFrom(
                        src => src.ReferenceType.HasValue
                            ? src.ReferenceType.ToString()
                            : null))
                .ForMember(
                    dest => dest.Items,
                    opt => opt.MapFrom(
                        src => src.WarehouseTaskItems));
            
            CreateMap<WarehouseTransferItem,WarehouseTransferItemResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                        src => src.Product.Name));
            
            CreateMap<WarehouseTransfer,WarehouseTransferResponseDto>()
                .ForMember(
                    dest => dest.Status,
                    opt => opt.MapFrom(
                        src => src.Status.ToString()))

                .ForMember(
                    dest => dest.SourceWarehouse,
                    opt => opt.MapFrom(
                        src => src.SourceWarehouse.Name))

                .ForMember(
                    dest => dest.DestinationWarehouse,
                    opt => opt.MapFrom(
                        src => src.DestinationWarehouse.Name))

                .ForMember(
                    dest => dest.CreatedBy,
                    opt => opt.MapFrom(
                        src => src.CreatedByUser.Name))
                 .ForMember(
                    dest => dest.Items,
                    opt => opt.MapFrom(
            src => src.WarehouseTransferItems));
                

            CreateMap<
                PurchaseOrderItem,
                PurchaseOrderItemResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                    src => src.Product.Name));

            CreateMap<
                PurchaseOrder,
                PurchaseOrderResponseDto>()
                .ForMember(
                    dest => dest.Status,
                    opt => opt.MapFrom(
                        src => src.Status.ToString()))

                .ForMember(
                    dest => dest.SupplierName,
                    opt => opt.MapFrom(
                        src => src.Supplier.Name))

                .ForMember(
                    dest => dest.WarehouseName,
                    opt => opt.MapFrom(
                        src => src.Warehouse.Name))

                .ForMember(
                    dest => dest.CreatedBy,
                    opt => opt.MapFrom(
                        src => src.CreatedByUser.Name))

                .ForMember(
                    dest => dest.Items,
                    opt => opt.MapFrom(
            src => src.PurchaseOrderItems));
            
            CreateMap<
                LowStockAlert,
                LowStockAlertResponseDto>()
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(
                        src => src.Product.Name))

                .ForMember(
                    dest => dest.SKU,
                    opt => opt.MapFrom(
                        src => src.Product.SKU))

                .ForMember(
                    dest => dest.WarehouseName,
                    opt => opt.MapFrom(
                        src => src.Warehouse.Name));
                
            CreateMap<CreateCompanyDto, Company>();

            CreateMap<UpdateCompanyDto, Company>();

            CreateMap<Company, CompanyResponseDto>();
                            
        }
    }
}