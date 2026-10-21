using EnterpriseERP.API.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseERP.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. System Settings
            if (!await context.SystemSettings.AnyAsync())
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    CompanyName = "مجموعة الأفق الذكية للتقنية والتجارة",
                    CompanyLogoUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150&auto=format&fit=crop&q=80",
                    TaxNumber = "310458923100003",
                    CurrencySymbol = "ج.م",
                    Phone = "+20 100 123 4567",
                    Email = "billing@alofoq-tech.com",
                    Address = "التجمع الخامس، القاهرة الجديدة، مصر",
                    CommercialRegister = "CR-1049283"
                });
                await context.SaveChangesAsync();
            }

            // 2. Suppliers
            if (!await context.Suppliers.AnyAsync())
            {
                context.Suppliers.AddRange(
                    new Supplier
                    {
                        SupplierName = "شركة النيل الدولية للتوريدات",
                        ContactPerson = "أحمد رضوان",
                        Phone = "+20 102 334 5566",
                        Email = "info@nilesupplies.com",
                        Address = "المنطقة الصناعية، العاشر من رمضان",
                        CreatedAt = DateTime.Now.AddDays(-60)
                    },
                    new Supplier
                    {
                        SupplierName = "مؤسسة الدلتا للأجهزة واللوجستيات",
                        ContactPerson = "طارق المنشاوي",
                        Phone = "+20 115 889 0011",
                        Email = "contact@deltalogistics.eg",
                        Address = "مدينة السادس من أكتوبر، الجيزة",
                        CreatedAt = DateTime.Now.AddDays(-45)
                    },
                    new Supplier
                    {
                        SupplierName = "مجموعة الفنار للحلول السحابية والإلكترونيات",
                        ContactPerson = "سارة عبد الرحمن",
                        Phone = "+20 120 771 2233",
                        Email = "sales@alfanar-tech.com",
                        Address = "القرية الذكية، طريق مصر إسكندرية الصحراوي",
                        CreatedAt = DateTime.Now.AddDays(-30)
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. Purchase Orders
            if (!await context.PurchaseOrders.AnyAsync())
            {
                var sup1 = await context.Suppliers.FirstOrDefaultAsync();
                var prd1 = await context.Products.FirstOrDefaultAsync();

                if (sup1 != null)
                {
                    var po1 = new PurchaseOrder
                    {
                        OrderNumber = "PO-20261001-001",
                        SupplierID = sup1.SupplierID,
                        OrderDate = DateTime.Now.AddDays(-20),
                        TotalAmount = 45000m,
                        Status = "Confirmed",
                        ProductID = prd1?.ProductID,
                        Quantity = 100,
                        UnitPrice = 450m,
                        Notes = "توريد دفعة بضائع رئيسية للربع الرابع"
                    };

                    var po2 = new PurchaseOrder
                    {
                        OrderNumber = "PO-20261010-002",
                        SupplierID = sup1.SupplierID,
                        OrderDate = DateTime.Now.AddDays(-10),
                        TotalAmount = 28000m,
                        Status = "Confirmed",
                        ProductID = prd1?.ProductID,
                        Quantity = 50,
                        UnitPrice = 560m,
                        Notes = "طلب توريد عاجل للمخزن المركزي"
                    };

                    context.PurchaseOrders.AddRange(po1, po2);
                    await context.SaveChangesAsync();
                }
            }

            // 4. Employees
            if (!await context.Employees.AnyAsync())
            {
                context.Employees.AddRange(
                    new Employee
                    {
                        FullName = "محمود عادل الشريف",
                        JobTitle = "مدير المبيعات والتسويق",
                        Department = "المبيعات",
                        BasicSalary = 18500m,
                        HireDate = DateTime.Now.AddMonths(-18),
                        Phone = "+20 101 223 9988",
                        Email = "m.adel@alofoq-tech.com",
                        IsActive = true
                    },
                    new Employee
                    {
                        FullName = "ياسمين كمال الدين",
                        JobTitle = "أخصائية موارد بشرية",
                        Department = "الموارد البشرية",
                        BasicSalary = 12000m,
                        HireDate = DateTime.Now.AddMonths(-12),
                        Phone = "+20 114 556 7788",
                        Email = "y.kamal@alofoq-tech.com",
                        IsActive = true
                    },
                    new Employee
                    {
                        FullName = "كريم سامي البربري",
                        JobTitle = "كبير محاسبين ومراقب مالي",
                        Department = "المالية والحسابات",
                        BasicSalary = 16000m,
                        HireDate = DateTime.Now.AddMonths(-24),
                        Phone = "+20 122 998 4433",
                        Email = "k.sami@alofoq-tech.com",
                        IsActive = true
                    },
                    new Employee
                    {
                        FullName = "عمر هاني النجار",
                        JobTitle = "مدير سلاسل الإمداد والمخازن",
                        Department = "اللوجستيات",
                        BasicSalary = 14500m,
                        HireDate = DateTime.Now.AddMonths(-10),
                        Phone = "+20 109 887 6655",
                        Email = "o.alnajar@alofoq-tech.com",
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 5. Payroll
            if (!await context.Payroll.AnyAsync())
            {
                var emp1 = await context.Employees.FirstOrDefaultAsync();
                if (emp1 != null)
                {
                    context.Payroll.AddRange(
                        new Payroll
                        {
                            EmployeeID = emp1.EmployeeID,
                            PaymentDate = DateTime.Now.AddDays(-25),
                            Bonus = 1500m,
                            Deductions = 500m,
                            NetSalary = emp1.BasicSalary + 1500m - 500m,
                            Status = "Paid",
                            Notes = "راتب شهر سبتمبر شامل مكافأة تحقيق الهدف"
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }

            // 6. Journal Entries
            if (!await context.JournalEntries.AnyAsync())
            {
                context.JournalEntries.AddRange(
                    new JournalEntry
                    {
                        TransactionDate = DateTime.Now.AddDays(-28),
                        Description = "فاتورة إيجار المقر الإداري والمستودعات",
                        Amount = 25000m,
                        Type = "Expense",
                        ReferenceNumber = "EXP-RENT-202609",
                        CreatedBy = "المحاسب المالي"
                    },
                    new JournalEntry
                    {
                        TransactionDate = DateTime.Now.AddDays(-15),
                        Description = "مصروفات كهرباء وخدمات مرافق وسيرفرات سحابية",
                        Amount = 7500m,
                        Type = "Expense",
                        ReferenceNumber = "EXP-UTIL-202610",
                        CreatedBy = "المحاسب المالي"
                    },
                    new JournalEntry
                    {
                        TransactionDate = DateTime.Now.AddDays(-8),
                        Description = "إيرادات استشارات وعقود صيانة دورية للعملاء",
                        Amount = 42000m,
                        Type = "Income",
                        ReferenceNumber = "INC-SRV-202610",
                        CreatedBy = "إدارة المبيعات"
                    }
                );
                await context.SaveChangesAsync();
            }

            // 7. Initial Audit Log
            if (!await context.AuditLogs.AnyAsync())
            {
                context.AuditLogs.Add(new AuditLog
                {
                    Action = "SYSTEM_INIT",
                    EntityName = "EnterpriseERP",
                    EntityID = "1",
                    Timestamp = DateTime.Now,
                    PerformedBy = "System Architect",
                    Details = "تم تهيئة وترقية نظام Enterprise ERP بكافة الوحدات الأساسية (المشتريات، المالية، الموارد البشرية، سجل التدقيق، الإعدادات)."
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
