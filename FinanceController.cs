using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;
using System.Globalization;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FinanceController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Finance/Summary
        [HttpGet("Summary")]
        public async Task<ActionResult<FinanceSummaryDto>> GetFinanceSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Date bounds
            var salesQuery = _context.SalesOrders.AsNoTracking().Where(s => s.Status != "Cancelled");
            var purchaseQuery = _context.PurchaseOrders.AsNoTracking().Where(p => p.Status != "Cancelled");
            var journalQuery = _context.JournalEntries.AsNoTracking().AsQueryable();
            var payrollQuery = _context.Payroll.AsNoTracking().Where(p => p.Status == "Paid");

            if (startDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.OrderDate >= startDate.Value);
                purchaseQuery = purchaseQuery.Where(p => p.OrderDate >= startDate.Value);
                journalQuery = journalQuery.Where(j => j.TransactionDate >= startDate.Value);
                payrollQuery = payrollQuery.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                salesQuery = salesQuery.Where(s => s.OrderDate <= eod);
                purchaseQuery = purchaseQuery.Where(p => p.OrderDate <= eod);
                journalQuery = journalQuery.Where(j => j.TransactionDate <= eod);
                payrollQuery = payrollQuery.Where(p => p.PaymentDate <= eod);
            }

            // Sales Revenue: Total sales from SalesOrders + manual Income Journal Entries
            var salesOrdersTotal = await salesQuery.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            var incomeJournalTotal = await journalQuery.Where(j => j.Type == "Income").SumAsync(j => (decimal?)j.Amount) ?? 0m;
            var totalSalesRevenue = salesOrdersTotal + incomeJournalTotal;

            // Purchase Costs: Total from PurchaseOrders (Confirmed/Received) + manual Purchase Journal Entries (excluding auto-generated)
            var purchaseOrdersTotal = await purchaseQuery.SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;
            var purchaseJournalTotal = await journalQuery.Where(j => j.Type == "Purchase" && (j.ReferenceNumber == null || !j.ReferenceNumber.StartsWith("PO-"))).SumAsync(j => (decimal?)j.Amount) ?? 0m;
            var totalPurchaseCosts = purchaseOrdersTotal + purchaseJournalTotal;

            // General Expenses: Journal Entries (Type: Expense) + Paid Employee Payroll
            var expensesJournalTotal = await journalQuery.Where(j => j.Type == "Expense" && (j.ReferenceNumber == null || !j.ReferenceNumber.StartsWith("PAY-"))).SumAsync(j => (decimal?)j.Amount) ?? 0m;
            var payrollTotal = await payrollQuery.SumAsync(p => (decimal?)p.NetSalary) ?? 0m;
            var totalExpenses = expensesJournalTotal + payrollTotal;

            // Calculated Net Profit / Loss = Revenue - Costs - Expenses
            var netProfit = totalSalesRevenue - totalPurchaseCosts - totalExpenses;
            var profitMargin = totalSalesRevenue > 0 
                ? Math.Round((netProfit / totalSalesRevenue) * 100, 2) 
                : 0m;

            // Monthly Trend Analysis for the last 6 months (Chart.js ready)
            var now = DateTime.Now;
            var monthlyTrends = new List<MonthlyTrendDto>();

            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var monthLabel = monthStart.ToString("MMM yyyy", new CultureInfo("ar-EG"));

                var mSales = await _context.SalesOrders
                    .Where(s => s.Status != "Cancelled" && s.OrderDate >= monthStart && s.OrderDate <= monthEnd)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
                var mIncome = await _context.JournalEntries
                    .Where(j => j.Type == "Income" && j.TransactionDate >= monthStart && j.TransactionDate <= monthEnd)
                    .SumAsync(j => (decimal?)j.Amount) ?? 0m;

                var mPurchases = await _context.PurchaseOrders
                    .Where(p => p.Status != "Cancelled" && p.OrderDate >= monthStart && p.OrderDate <= monthEnd)
                    .SumAsync(p => (decimal?)p.TotalAmount) ?? 0m;

                var mExpenses = await _context.JournalEntries
                    .Where(j => j.Type == "Expense" && (j.ReferenceNumber == null || !j.ReferenceNumber.StartsWith("PAY-")) && j.TransactionDate >= monthStart && j.TransactionDate <= monthEnd)
                    .SumAsync(j => (decimal?)j.Amount) ?? 0m;
                var mPayroll = await _context.Payroll
                    .Where(p => p.Status == "Paid" && p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                    .SumAsync(p => (decimal?)p.NetSalary) ?? 0m;

                var mRev = mSales + mIncome;
                var mExp = mExpenses + mPayroll;
                var mNet = mRev - mPurchases - mExp;

                monthlyTrends.Add(new MonthlyTrendDto
                {
                    Month = monthLabel,
                    Revenue = mRev,
                    Purchases = mPurchases,
                    Expenses = mExp,
                    NetProfit = mNet
                });
            }

            // Recent 10 Journal Entries
            var recentEntries = await _context.JournalEntries
                .AsNoTracking()
                .OrderByDescending(j => j.TransactionDate)
                .Take(10)
                .ToListAsync();

            return new FinanceSummaryDto
            {
                TotalSalesRevenue = totalSalesRevenue,
                TotalPurchaseCosts = totalPurchaseCosts,
                GeneralExpenses = totalExpenses,
                NetProfit = netProfit,
                ProfitMarginPercent = profitMargin,
                MonthlyTrends = monthlyTrends,
                RecentEntries = recentEntries
            };
        }

        // 2. GET: api/Finance/Entries - Query Journal Entries with filters
        [HttpGet("Entries")]
        public async Task<ActionResult<IEnumerable<JournalEntry>>> GetJournalEntries(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? type = null,
            [FromQuery] string? search = null)
        {
            var query = _context.JournalEntries.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(j => j.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(j => j.TransactionDate <= eod);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim().ToLower();
                query = query.Where(j => j.Type.ToLower() == t);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(j => 
                    j.Description.ToLower().Contains(s) || 
                    (j.ReferenceNumber != null && j.ReferenceNumber.ToLower().Contains(s)) ||
                    (j.CreatedBy != null && j.CreatedBy.ToLower().Contains(s)));
            }

            return await query.OrderByDescending(j => j.TransactionDate).ThenByDescending(j => j.EntryID).ToListAsync();
        }

        // 3. GET: api/Finance/Entries/5
        [HttpGet("Entries/{id:int}")]
        public async Task<ActionResult<JournalEntry>> GetJournalEntry(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف القيد غير صالح." });
            }

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound(new { message = $"القيد المالي #{id} غير موجود." });
            }

            return entry;
        }

        // 4. POST: api/Finance/Entries
        [HttpPost("Entries")]
        public async Task<ActionResult<JournalEntry>> PostJournalEntry([FromBody] JournalEntry entry)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(entry.Description))
            {
                return BadRequest(new { message = "بيان القيد المالي مطلوب إجبارياً." });
            }

            if (entry.Amount <= 0)
            {
                return BadRequest(new { message = "مبلغ القيد يجب أن يكون أكبر من صفر." });
            }

            if (string.IsNullOrWhiteSpace(entry.Type))
            {
                entry.Type = "Expense";
            }

            if (entry.TransactionDate == default)
            {
                entry.TransactionDate = DateTime.Now;
            }

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "JournalEntry",
                entry.EntryID.ToString(),
                $"تم إضافة قيد مالي ({entry.Type}): {entry.Description} بمبلغ {entry.Amount:N2} ج.م"
            );

            return CreatedAtAction(nameof(GetJournalEntry), new { id = entry.EntryID }, entry);
        }

        // 5. PUT: api/Finance/Entries/5 - Safe scalar update
        [HttpPut("Entries/{id:int}")]
        public async Task<IActionResult> PutJournalEntry(int id, [FromBody] JournalEntry entry)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف القيد غير صالح." });
            }

            var existing = await _context.JournalEntries.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"القيد المالي #{id} غير موجود." });
            }

            if (entry.Amount <= 0)
            {
                return BadRequest(new { message = "مبلغ القيد يجب أن يكون أكبر من صفر." });
            }

            entry.EntryID = id;
            if (entry.TransactionDate == default)
            {
                entry.TransactionDate = existing.TransactionDate;
            }

            _context.Entry(existing).CurrentValues.SetValues(entry);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "JournalEntry",
                id.ToString(),
                $"تم تعديل القيد المالي #{id}: {entry.Description} بمبلغ {entry.Amount:N2} ج.م"
            );

            return NoContent();
        }

        // 6. DELETE: api/Finance/Entries/5
        [HttpDelete("Entries/{id:int}")]
        public async Task<IActionResult> DeleteJournalEntry(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف القيد غير صالح." });
            }

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null)
            {
                return NotFound(new { message = $"القيد المالي #{id} غير موجود." });
            }

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "JournalEntry",
                id.ToString(),
                $"تم حذف القيد المالي #{id}: {entry.Description}"
            );

            return NoContent();
        }
    }

    public class FinanceSummaryDto
    {
        public decimal TotalSalesRevenue { get; set; }
        public decimal TotalPurchaseCosts { get; set; }
        public decimal GeneralExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
        public List<JournalEntry> RecentEntries { get; set; } = new();
    }

    public class MonthlyTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Purchases { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
    }
}
