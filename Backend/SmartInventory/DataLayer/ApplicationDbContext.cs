using Microsoft.EntityFrameworkCore;
using SmartInventoryManagement.Models;

namespace SmartInventoryManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Warehouse> Warehouses { get; set; }

        public DbSet<Inventory> Inventories { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }

        public DbSet<WarehouseTransferItem> WarehouseTransferItems { get; set; }

        public DbSet<StockMovement> StockMovements { get; set; }

        public DbSet<LowStockAlert> LowStockAlerts { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<WarehouseTask> WarehouseTasks { get; set; }

        public DbSet<WarehouseTaskItem> WarehouseTaskItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USER

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(255);

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AssignedWarehouse)
                .WithMany()
                .HasForeignKey(u => u.AssignedWarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<User>()
                .HasOne(u => u.Supplier)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);



            // ROLE

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // SEED DATA
            
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "Admin"
                },
                new Role
                {
                    Id = 2,
                    Name = "WarehouseManager"
                },
                new Role
                {
                    Id = 3,
                    Name = "InventoryStaff"
                },
                new Role
                {
                    Id = 4,
                    Name = "Supplier"
                }
            );

            modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "System Admin",
                Email = "admin@inventory.com",
                PasswordHash = "$2a$12$WkkmkRdWtv9e.HoO47BZRuQ1mCQX8rjkA4Rx.BCr5VurebMnvPCbS",
                RoleId = 1,
                IsPasswordSet = true
            }
        );
            // PRODUCT

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();
            
            modelBuilder.Entity<Product>()
                .HasIndex(p=>p.ModelNumber)
                .IsUnique();
            
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Inventories)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Product>()
                .HasMany(p => p.PurchaseOrderItems)
                .WithOne(poi => poi.Product)
                .HasForeignKey(poi => poi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.WarehouseTransferItems)
                .WithOne(wti => wti.Product)
                .HasForeignKey(wti => wti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.StockMovements)
                .WithOne(sm => sm.Product)
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<Product>()
                .Property(p => p.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.RequiredStorageType)
                .HasConversion<string>();

            modelBuilder.Entity<Product>()
                .Property(p => p.Length)
                .HasPrecision(18,4);

            modelBuilder.Entity<Product>()
                .Property(p => p.Width)
                .HasPrecision(18,4);

            modelBuilder.Entity<Product>()
                .Property(p => p.Height)
                .HasPrecision(18,4);

            modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

            // WAREHOUSE

            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => w.Name)
                .IsUnique();

            modelBuilder.Entity<Warehouse>()
                .Property(w => w.StorageType)
                .HasConversion<string>();


            modelBuilder.Entity<Warehouse>()
                .Property(w => w.Capacity)
                .HasPrecision(18,4);

            modelBuilder.Entity<Warehouse>()
                .Property(w => w.AvailableCapacity)
                .HasPrecision(18,4);

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.ReferenceType)
                .HasConversion<string>();

            // INVENTORY

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new
                {
                    i.ProductId,
                    i.WarehouseId
                })
                .IsUnique();

            modelBuilder.Entity<Inventory>()
                .Property(i => i.LastUpdated)
                .HasColumnType("timestamp without time zone");
            
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // PURCHASE ORDER

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(po => po.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .Property(po => po.OrderedDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(po => po.ReceivedDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<PurchaseOrderItem>()
                .Property(poi => poi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Warehouse)
                .WithMany()
                .HasForeignKey(po => po.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // WAREHOUSE TRANSFER

            modelBuilder.Entity<WarehouseTransfer>()
                .Property(wt => wt.TransferDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(wt => wt.SourceWarehouse)
                .WithMany(w => w.SourceTransfers)
                .HasForeignKey(wt => wt.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(wt => wt.DestinationWarehouse)
                .WithMany(w => w.DestinationTransfers)
                .HasForeignKey(wt => wt.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasIndex(wt => wt.TransferNumber)
                .IsUnique();
            modelBuilder.Entity<WarehouseTransfer>()
                .Property(wt => wt.Reason)
                .HasMaxLength(500);
            modelBuilder.Entity<WarehouseTransfer>()
                .Property(wt => wt.CompletedDate)
                .HasColumnType("timestamp without time zone");

            // STOCK MOVEMENT

            modelBuilder.Entity<StockMovement>()
                .Property(sm => sm.CreatedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.CreatedByUser)
            .WithMany()
            .HasForeignKey(sm => sm.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
            .Property(sm => sm.Type)
            .HasConversion<string>();

            // LOW STOCK ALERT

            modelBuilder.Entity<LowStockAlert>()
                .Property(lsa => lsa.CreatedAt)
                .HasColumnType("timestamp without time zone");

            
            // SUPPLIER
            
            modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Email)
            .IsUnique();


            // COMPANY

            modelBuilder.Entity<Company>()
            .HasIndex(c => c.Name)
            .IsUnique();

            modelBuilder.Entity<Product>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

            // WAREHOUSE TASK

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.Type)
                .HasConversion<string>();

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.Status)
                .HasConversion<string>();

            modelBuilder.Entity<WarehouseTask>()
                .HasOne(wt => wt.CreatedByUser)
                .WithMany()
                .HasForeignKey(wt => wt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTask>()
                .HasOne(wt => wt.StartedByUser)
                .WithMany()
                .HasForeignKey(wt => wt.StartedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTask>()
                .HasOne(wt => wt.CompletedByUser)
                .WithMany()
                .HasForeignKey(wt => wt.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.CreatedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.StartedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.CompletedAt)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<WarehouseTask>()
                .HasOne(t => t.Warehouse)
                .WithMany()
                .HasForeignKey(t => t.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<WarehouseTask>()
                .Property(wt => wt.ReferenceType)
                .HasConversion<string>();
            


            // WAREHOUSE TASK ITEM

            modelBuilder.Entity<WarehouseTaskItem>()
                .HasOne(ti => ti.WarehouseTask)
                .WithMany(t => t.WarehouseTaskItems)
                .HasForeignKey(ti => ti.WarehouseTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseTaskItem>()
                .HasOne(ti => ti.Product)
                .WithMany()
                .HasForeignKey(ti => ti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                            

        }
    }
}