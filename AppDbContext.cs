using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseERP.API.Data
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal CreditLimit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "كمية المخزون يجب ألا تكون سالبة")]
        public int StockQuantity { get; set; } = 0;
        public int ReorderLevel { get; set; } = 10;
        public bool IsActive { get; set; } = true;
    }

    public class Warehouse
    {
        [Key]
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    public class ProductStock
    {
        [Key]
        public int StockID { get; set; }
        public int ProductID { get; set; }
        public int WarehouseID { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
    }

    public class SalesOrder
    {
        [Key]
        public int OrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int CustomerID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Confirmed";
        public decimal TotalAmount { get; set; }
    }

    public class SalesOrderItem
    {
        [Key]
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
    }

    // MODULE 1: Suppliers & Purchases
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class PurchaseOrder
    {
        [Key]
        public int PurchaseOrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Confirmed"; // Confirmed, Pending, Received, Cancelled
        public int? ProductID { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderItem
    {
        [Key]
        public int PurchaseOrderItemID { get; set; }
        public int PurchaseOrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // MODULE 2: Accounting & Finance
    public class JournalEntry
    {
        [Key]
        public int EntryID { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Expense"; // Income, Expense, Purchase, Sale
        public string? ReferenceNumber { get; set; }
        public string? CreatedBy { get; set; } = "System Admin";
    }

    // MODULE 3: HR & Payroll
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public DateTime HireDate { get; set; } = DateTime.Now;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Payroll
    {
        [Key]
        public int PayrollID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Bonus { get; set; } = 0;
        public decimal Deductions { get; set; } = 0;
        public decimal NetSalary { get; set; }
        public string Status { get; set; } = "Paid"; // Paid, Pending
        public string? Notes { get; set; }
    }

    // MODULE 4: Audit Logs & System Settings
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, STOCK_SYNC
        public string EntityName { get; set; } = string.Empty; // Product, Customer, Supplier, SalesOrder, etc.
        public string EntityID { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string PerformedBy { get; set; } = "System Admin";
        public string? Details { get; set; }
    }

    public class SystemSetting
    {
        [Key]
        public int SettingID { get; set; }
        public string CompanyName { get; set; } = "مؤسسة الأفق الذكية للتجارة والخدمات اللوجستية";
        public string? CompanyLogoUrl { get; set; }
        public string? TaxNumber { get; set; } = "310458923100003";
        public string? CurrencySymbol { get; set; } = "ج.م";
        public string? Phone { get; set; } = "+20 100 123 4567";
        public string? Email { get; set; } = "info@alofoq-erp.com";
        public string? Address { get; set; } = "القاهرة، مصر";
        public string? CommercialRegister { get; set; } = "1049283";
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<ProductStock> ProductStock => Set<ProductStock>();
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();

        // New Core Business Modules
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Payroll> Payroll => Set<Payroll>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Decimal precision for SQL Server
            modelBuilder.Entity<Customer>().Property(c => c.CreditLimit).HasPrecision(18, 2);
            
            modelBuilder.Entity<Product>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(18, 2);
            
            modelBuilder.Entity<ProductStock>().Property(ps => ps.QuantityOnHand).HasPrecision(18, 2);
            modelBuilder.Entity<ProductStock>().Property(ps => ps.QuantityReserved).HasPrecision(18, 2);
            
            modelBuilder.Entity<SalesOrder>().Property(so => so.TotalAmount).HasPrecision(18, 2);
            
            modelBuilder.Entity<SalesOrderItem>().Property(soi => soi.Quantity).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrderItem>().Property(soi => soi.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<SalesOrderItem>().Property(soi => soi.Discount).HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseOrder>().Property(po => po.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(po => po.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrderItem>().Property(poi => poi.UnitPrice).HasPrecision(18, 2);

            modelBuilder.Entity<JournalEntry>().Property(je => je.Amount).HasPrecision(18, 2);

            modelBuilder.Entity<Employee>().Property(e => e.BasicSalary).HasPrecision(18, 2);

            modelBuilder.Entity<Payroll>().Property(p => p.Bonus).HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>().Property(p => p.Deductions).HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>().Property(p => p.NetSalary).HasPrecision(18, 2);
        }
    }
}
