using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartInventoryManagement.API.Middlewares;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.BusinessLayer.Mappings;
using SmartInventoryManagement.BusinessLayer.Services;
using SmartInventoryManagement.Data;
using SmartInventoryManagement.DataLayer.Interfaces;
using SmartInventoryManagement.DataLayer.Repositories;
using SmartInventoryManagement.Models;
using SmartInventoryManagement.Models.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
#region Contexts
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});
#endregion

builder.Services.AddAutoMapper(
    typeof(MappingProfile));

 builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(
        "EmailSettings"));

#region Services

    builder.Services.AddScoped<IAuthService,AuthService>();

    builder.Services.AddScoped<IUserService,UserService>();

    builder.Services.AddScoped<ITokenService,TokenService>();

    builder.Services.AddScoped<IWarehouseService,WarehouseService>();

    builder.Services.AddScoped<IEmailService,EmailService>();

    builder.Services.AddScoped<ICategoryService,CategoryService>();

    builder.Services.AddScoped<IProductService,ProductService>();

    builder.Services.AddScoped<IInventoryService,InventoryService>();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<ICurrentUserService,CurrentUserService>();

    builder.Services.AddScoped<IStockMovementService,StockMovementService>();

    builder.Services.AddScoped<IWarehouseTaskService,WarehouseTaskService>();

    builder.Services.AddScoped<IWarehouseTransferService,WarehouseTransferService>();

    builder.Services.AddScoped<IPurchaseOrderService,PurchaseOrderService>();

    builder.Services.AddScoped<ILowStockAlertService,LowStockAlertService>();

    builder.Services.AddScoped<ISupplierOrderService,SupplierOrderService>();

    builder.Services.AddScoped<ICompanyService,CompanyService>();

#endregion

#region Repositories

    builder.Services.AddScoped<IUserRepository,UserRepository>();

    builder.Services.AddScoped<IRepository<Role>,Repository<Role>>();

    builder.Services.AddScoped<IRepository<Warehouse>,Repository<Warehouse>>();

    builder.Services.AddScoped<IRepository<Category>, Repository<Category>>();

    builder.Services.AddScoped<IProductRepository, ProductRepository>();

    builder.Services.AddScoped<IRepository<Company>, Repository<Company>>();

    builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

    builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();

    builder.Services.AddScoped<IWarehouseTaskRepository,WarehouseTaskRepository>();

    builder.Services.AddScoped<IWarehouseTransferRepository,WarehouseTransferRepository>();

    builder.Services.AddScoped<IPurchaseOrderRepository,PurchaseOrderRepository>();

    builder.Services.AddScoped<ILowStockAlertRepository,LowStockAlertRepository>();

    builder.Services.AddScoped<IRepository<Supplier>, Repository<Supplier>>();


#endregion
    builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["JWT:Issuer"],
                ValidAudience = builder.Configuration["JWT:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["JWT:Key"]!))
            };
    });
    builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();

    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

