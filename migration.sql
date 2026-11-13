IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [Customers] (
        [CustomerID] int NOT NULL IDENTITY,
        [CustomerName] nvarchar(max) NOT NULL,
        [TaxNumber] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [CreditLimit] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [Products] (
        [ProductID] int NOT NULL IDENTITY,
        [SKU] nvarchar(max) NOT NULL,
        [ProductName] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [CostPrice] decimal(18,2) NOT NULL,
        [ReorderLevel] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([ProductID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [ProductStock] (
        [StockID] int NOT NULL IDENTITY,
        [ProductID] int NOT NULL,
        [WarehouseID] int NOT NULL,
        [QuantityOnHand] decimal(18,2) NOT NULL,
        [QuantityReserved] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ProductStock] PRIMARY KEY ([StockID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [SalesOrderItems] (
        [OrderItemID] int NOT NULL IDENTITY,
        [OrderID] int NOT NULL,
        [ProductID] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SalesOrderItems] PRIMARY KEY ([OrderItemID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [SalesOrders] (
        [OrderID] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(max) NOT NULL,
        [CustomerID] int NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([OrderID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [WarehouseID] int NOT NULL IDENTITY,
        [WarehouseName] nvarchar(max) NOT NULL,
        [Location] nvarchar(max) NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([WarehouseID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016145608_InitialCreate_V2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016145608_InitialCreate_V2', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016152635_AddDecimalPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016152635_AddDecimalPrecision', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019204949_AddStockQuantityToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [StockQuantity] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019204949_AddStockQuantityToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019204949_AddStockQuantityToProduct', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [LogID] int NOT NULL IDENTITY,
        [Action] nvarchar(max) NOT NULL,
        [EntityName] nvarchar(max) NOT NULL,
        [EntityID] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [PerformedBy] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([LogID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [Employees] (
        [EmployeeID] int NOT NULL IDENTITY,
        [FullName] nvarchar(max) NOT NULL,
        [JobTitle] nvarchar(max) NOT NULL,
        [Department] nvarchar(max) NOT NULL,
        [BasicSalary] decimal(18,2) NOT NULL,
        [HireDate] datetime2 NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [JournalEntries] (
        [EntryID] int NOT NULL IDENTITY,
        [TransactionDate] datetime2 NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [ReferenceNumber] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([EntryID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [Payroll] (
        [PayrollID] int NOT NULL IDENTITY,
        [EmployeeID] int NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Bonus] decimal(18,2) NOT NULL,
        [Deductions] decimal(18,2) NOT NULL,
        [NetSalary] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_Payroll] PRIMARY KEY ([PayrollID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [PurchaseOrderItems] (
        [PurchaseOrderItemID] int NOT NULL IDENTITY,
        [PurchaseOrderID] int NOT NULL,
        [ProductID] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([PurchaseOrderItemID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [PurchaseOrderID] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(max) NOT NULL,
        [SupplierID] int NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ProductID] int NULL,
        [Quantity] int NULL,
        [UnitPrice] decimal(18,2) NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([PurchaseOrderID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [SupplierID] int NOT NULL IDENTITY,
        [SupplierName] nvarchar(max) NOT NULL,
        [ContactPerson] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([SupplierID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [SettingID] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(max) NOT NULL,
        [CompanyLogoUrl] nvarchar(max) NULL,
        [TaxNumber] nvarchar(max) NULL,
        [CurrencySymbol] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [CommercialRegister] nvarchar(max) NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([SettingID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261020161815_AddEnterpriseModules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261020161815_AddEnterpriseModules', N'10.0.13');
END;

COMMIT;
GO



-- ==============================================================================
-- ENTERPRISE ERP: READY-TO-DEMO IDEMPOTENT FOUNDATIONAL SEED DATA
-- ==============================================================================
BEGIN TRANSACTION;

-- 1. System Settings
IF NOT EXISTS (SELECT 1 FROM [SystemSettings])
BEGIN
    INSERT INTO [SystemSettings] ([CompanyName], [CompanyLogoUrl], [TaxNumber], [CurrencySymbol], [Phone], [Email], [Address], [CommercialRegister])
    VALUES (N'مجموعة الأفق الذكية للتقنية والتجارة', N'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150&auto=format&fit=crop&q=80', N'310458923100003', N'ج.م', N'+20 100 123 4567', N'billing@alofoq-tech.com', N'التجمع الخامس، القاهرة الجديدة، مصر', N'CR-1049283');
END;

-- 2. Warehouses
IF NOT EXISTS (SELECT 1 FROM [Warehouses])
BEGIN
    INSERT INTO [Warehouses] ([WarehouseName], [Location])
    VALUES 
    (N'المستودع الرئيسي - العاشر من رمضان', N'المنطقة الصناعية B4، العاشر من رمضان'),
    (N'مستودع التوزيع السريع - السادس من أكتوبر', N'المنطقة الصناعية الثالثة، 6 أكتوبر'),
    (N'مستودع الدلتا اللوجستي - طنطا', N'طريق القاهرة الإسكندرية الزراعي، طنطا');
END;

-- 3. Suppliers
IF NOT EXISTS (SELECT 1 FROM [Suppliers])
BEGIN
    INSERT INTO [Suppliers] ([SupplierName], [ContactPerson], [Phone], [Email], [Address], [CreatedAt])
    VALUES 
    (N'شركة النيل الدولية للتوريدات', N'أحمد رضوان', N'+20 102 334 5566', N'info@nilesupplies.com', N'المنطقة الصناعية، العاشر من رمضان', DATEADD(day, -60, GETDATE())),
    (N'مؤسسة الدلتا للأجهزة واللوجستيات', N'طارق المنشاوي', N'+20 115 889 0011', N'contact@deltalogistics.eg', N'مدينة السادس من أكتوبر، الجيزة', DATEADD(day, -45, GETDATE())),
    (N'مجموعة الفنار للحلول السحابية والإلكترونيات', N'سارة عبد الرحمن', N'+20 120 771 2233', N'sales@alfanar-tech.com', N'القرية الذكية، طريق مصر إسكندرية الصحراوي', DATEADD(day, -30, GETDATE()));
END;

-- 4. Products
IF NOT EXISTS (SELECT 1 FROM [Products])
BEGIN
    INSERT INTO [Products] ([SKU], [ProductName], [UnitPrice], [CostPrice], [StockQuantity], [ReorderLevel], [IsActive])
    VALUES 
    (N'SRV-DELL-R750', N'خادم Dell PowerEdge R750 Enterprise Server', 185000.00, 142000.00, 15, 3, 1),
    (N'NET-CISCO-C9300', N'محول شبكات Cisco Catalyst 9300 48-Port PoE+', 92000.00, 71000.00, 28, 5, 1),
    (N'UPS-APC-10KVA', N'مزود طاقة ذكي APC Smart-UPS RT 10kVA', 64000.00, 48000.00, 20, 4, 1),
    (N'LAP-THINK-P16', N'محطة عمل محمولة Lenovo ThinkPad P16 Gen 2', 78000.00, 61000.00, 45, 10, 1),
    (N'SAN-SYN-FS6400', N'وحدة تخزين شبكي Synology FlashStation FS6400', 245000.00, 195000.00, 8, 2, 1),
    (N'FIRE-FORTI-100F', N'جدار حماية ذكي Fortinet FortiGate 100F NGFW', 83000.00, 64000.00, 32, 6, 1);
END;

-- 5. Customers
IF NOT EXISTS (SELECT 1 FROM [Customers])
BEGIN
    INSERT INTO [Customers] ([CustomerName], [TaxNumber], [Phone], [Email], [CreditLimit], [CreatedAt])
    VALUES 
    (N'شركة البنك الأهلي للخدمات الرقمية', N'200192834', N'+20 100 998 8776', N'procurement@nbe-digital.eg', 1500000.00, DATEADD(day, -90, GETDATE())),
    (N'مستشفيات كليوباترا التخصصية', N'341209845', N'+20 111 223 3445', N'it@cleopatra-hospitals.com', 850000.00, DATEADD(day, -75, GETDATE())),
    (N'شركة السويدي للحلول الكهربائية', N'109283746', N'+20 122 445 5667', N'supply@elsewedy-solutions.com', 2000000.00, DATEADD(day, -60, GETDATE())),
    (N'مجموعة طلعت مصطفى للتطوير العقاري', N'450918273', N'+20 106 778 8990', N'contracts@tmg-holding.com', 3000000.00, DATEADD(day, -45, GETDATE()));
END;

-- 6. Employees
IF NOT EXISTS (SELECT 1 FROM [Employees])
BEGIN
    INSERT INTO [Employees] ([FullName], [JobTitle], [Department], [BasicSalary], [HireDate], [Phone], [Email], [IsActive])
    VALUES 
    (N'محمود عادل الشريف', N'مدير المبيعات والتسويق', N'المبيعات', 18500.00, DATEADD(month, -18, GETDATE()), N'+20 101 223 9988', N'm.adel@alofoq-tech.com', 1),
    (N'ياسمين كمال الدين', N'أخصائية موارد بشرية', N'الموارد البشرية', 12000.00, DATEADD(month, -12, GETDATE()), N'+20 114 556 7788', N'y.kamal@alofoq-tech.com', 1),
    (N'كريم سامي البربري', N'كبير محاسبين ومراقب مالي', N'المالية والحسابات', 16000.00, DATEADD(month, -24, GETDATE()), N'+20 122 998 4433', N'k.sami@alofoq-tech.com', 1),
    (N'عمر هاني النجار', N'مدير سلاسل الإمداد والمخازن', N'اللوجستيات', 14500.00, DATEADD(month, -10, GETDATE()), N'+20 109 887 6655', N'o.alnajar@alofoq-tech.com', 1);
END;

-- 7. Payroll
IF NOT EXISTS (SELECT 1 FROM [Payroll])
BEGIN
    DECLARE @EmpID INT = (SELECT TOP 1 [EmployeeID] FROM [Employees]);
    IF @EmpID IS NOT NULL
    BEGIN
        INSERT INTO [Payroll] ([EmployeeID], [PaymentDate], [Bonus], [Deductions], [NetSalary], [Status], [Notes])
        VALUES (@EmpID, DATEADD(day, -25, GETDATE()), 1500.00, 500.00, 19500.00, N'Paid', N'راتب شهر سبتمبر شامل مكافأة تحقيق الهدف');
    END;
END;

-- 8. Purchase Orders
IF NOT EXISTS (SELECT 1 FROM [PurchaseOrders])
BEGIN
    DECLARE @SupID INT = (SELECT TOP 1 [SupplierID] FROM [Suppliers]);
    DECLARE @PrdID INT = (SELECT TOP 1 [ProductID] FROM [Products]);
    IF @SupID IS NOT NULL
    BEGIN
        INSERT INTO [PurchaseOrders] ([OrderNumber], [SupplierID], [OrderDate], [TotalAmount], [Status], [ProductID], [Quantity], [UnitPrice], [Notes])
        VALUES 
        (N'PO-20261001-001', @SupID, DATEADD(day, -20, GETDATE()), 45000.00, N'Confirmed', @PrdID, 100, 450.00, N'توريد دفعة بضائع رئيسية للربع الرابع'),
        (N'PO-20261010-002', @SupID, DATEADD(day, -10, GETDATE()), 28000.00, N'Confirmed', @PrdID, 50, 560.00, N'طلب توريد عاجل للمخزن المركزي');
    END;
END;

-- 9. Sales Orders
IF NOT EXISTS (SELECT 1 FROM [SalesOrders])
BEGIN
    DECLARE @CustID INT = (SELECT TOP 1 [CustomerID] FROM [Customers]);
    IF @CustID IS NOT NULL
    BEGIN
        INSERT INTO [SalesOrders] ([OrderNumber], [CustomerID], [OrderDate], [Status], [TotalAmount])
        VALUES 
        (N'SO-20261012-001', @CustID, DATEADD(day, -14, GETDATE()), N'Confirmed', 185000.00),
        (N'SO-20261018-002', @CustID, DATEADD(day, -7, GETDATE()), N'Confirmed', 92000.00),
        (N'SO-20261022-003', @CustID, DATEADD(day, -2, GETDATE()), N'Confirmed', 156000.00);
    END;
END;

-- 10. Journal Entries (Finance & Revenue Dashboard)
IF NOT EXISTS (SELECT 1 FROM [JournalEntries])
BEGIN
    INSERT INTO [JournalEntries] ([TransactionDate], [Description], [Amount], [Type], [ReferenceNumber], [CreatedBy])
    VALUES 
    (DATEADD(day, -28, GETDATE()), N'فاتورة إيجار المقر الإداري والمستودعات', 25000.00, N'Expense', N'EXP-RENT-202609', N'المحاسب المالي'),
    (DATEADD(day, -15, GETDATE()), N'مصروفات كهرباء وخدمات مرافق وسيرفرات سحابية', 7500.00, N'Expense', N'EXP-UTIL-202610', N'المحاسب المالي'),
    (DATEADD(day, -8, GETDATE()), N'إيرادات استشارات وعقود صيانة دورية للعملاء', 42000.00, N'Income', N'INC-SRV-202610', N'إدارة المبيعات');
END;

-- 11. Initial Audit Log
IF NOT EXISTS (SELECT 1 FROM [AuditLogs])
BEGIN
    INSERT INTO [AuditLogs] ([Action], [EntityName], [EntityID], [Timestamp], [PerformedBy], [Details])
    VALUES (N'SYSTEM_INIT', N'EnterpriseERP', N'1', GETDATE(), N'Principal Cloud Architect', N'تم ترقية ونشر النظام السحابي بنجاح على منصة MonsterASP.NET وقاعدة بيانات MS SQL Server 2025.');
END;

COMMIT;
GO
