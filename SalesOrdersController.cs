using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/SalesOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrders(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var query = _context.SalesOrders.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= eod);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToLower();
                query = query.Where(o => o.Status.ToLower() == st);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => 
                    o.OrderNumber.ToLower().Contains(s) ||
                    o.CustomerID.ToString().Contains(s));
            }

            return await query.OrderByDescending(o => o.OrderID).ToListAsync();
        }

        // 2. GET: api/SalesOrders/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesOrder>> GetSalesOrder(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر البيع غير صالح." });
            }

            var order = await _context.SalesOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"أمر البيع #{id} غير موجود." });
            }

            return order;
        }

        // 3. POST: api/SalesOrders
        [HttpPost]
        public async Task<ActionResult<SalesOrder>> PostSalesOrder(SalesOrder salesOrder)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify Customer exists
            if (salesOrder.CustomerID > 0)
            {
                var customerExists = await _context.Customers.AnyAsync(c => c.CustomerID == salesOrder.CustomerID);
                if (!customerExists)
                {
                    return BadRequest(new { message = $"العميل ذو المعرّف #{salesOrder.CustomerID} غير موجود." });
                }
            }
            else
            {
                return BadRequest(new { message = "يجب تحديد العميل أولاً." });
            }

            if (salesOrder.OrderDate == default)
            {
                salesOrder.OrderDate = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(salesOrder.Status))
            {
                salesOrder.Status = "Confirmed";
            }

            if (string.IsNullOrWhiteSpace(salesOrder.OrderNumber))
            {
                salesOrder.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
            }

            _context.SalesOrders.Add(salesOrder);
            await _context.SaveChangesAsync();

            // Automatic Journal Entry for confirmed sales
            if (string.Equals(salesOrder.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                var journalEntry = new JournalEntry
                {
                    TransactionDate = salesOrder.OrderDate,
                    Description = $"إيراد أمر بيع #{salesOrder.OrderNumber}",
                    Amount = salesOrder.TotalAmount,
                    Type = "Sale",
                    ReferenceNumber = salesOrder.OrderNumber,
                    CreatedBy = "Sales System Sync"
                };
                _context.JournalEntries.Add(journalEntry);
                await _context.SaveChangesAsync();
            }

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "SalesOrder",
                salesOrder.OrderID.ToString(),
                $"تم إنشاء أمر بيع جديد #{salesOrder.OrderNumber} للعميل #{salesOrder.CustomerID} بقيمة {salesOrder.TotalAmount:N2} ج.م"
            );

            return CreatedAtAction(nameof(GetSalesOrder), new { id = salesOrder.OrderID }, salesOrder);
        }

        // 4. PUT: api/SalesOrders/5 - Explicit scalar tracking update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutSalesOrder(int id, SalesOrder salesOrder)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر البيع غير صالح." });
            }

            var existing = await _context.SalesOrders.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"أمر البيع #{id} غير موجود." });
            }

            // Verify Customer exists if changed
            if (salesOrder.CustomerID > 0 && salesOrder.CustomerID != existing.CustomerID)
            {
                var customerExists = await _context.Customers.AnyAsync(c => c.CustomerID == salesOrder.CustomerID);
                if (!customerExists)
                {
                    return BadRequest(new { message = $"العميل ذو المعرّف #{salesOrder.CustomerID} غير موجود." });
                }
            }

            salesOrder.OrderID = id;
            if (salesOrder.OrderDate == default)
            {
                salesOrder.OrderDate = existing.OrderDate;
            }
            if (string.IsNullOrWhiteSpace(salesOrder.OrderNumber))
            {
                salesOrder.OrderNumber = existing.OrderNumber;
            }
            if (string.IsNullOrWhiteSpace(salesOrder.Status))
            {
                salesOrder.Status = existing.Status;
            }

            // Map scalar fields directly to avoid tracking conflict or missing navigation properties
            _context.Entry(existing).CurrentValues.SetValues(salesOrder);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "SalesOrder",
                id.ToString(),
                $"تم تحديث أمر البيع #{existing.OrderNumber} (الحالة: {salesOrder.Status})"
            );

            return NoContent(); // 204
        }

        // 5. DELETE: api/SalesOrders/5 - Graceful cascade deletion of order items
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSalesOrder(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر البيع غير صالح." });
            }

            var order = await _context.SalesOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"أمر البيع #{id} غير موجود." });
            }

            // Cascade delete child order items if any exist
            var orderItems = await _context.SalesOrderItems.Where(i => i.OrderID == id).ToListAsync();
            if (orderItems.Count > 0)
            {
                _context.SalesOrderItems.RemoveRange(orderItems);
            }

            _context.SalesOrders.Remove(order);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "SalesOrder",
                id.ToString(),
                $"تم حذف أمر البيع #{order.OrderNumber}"
            );

            return NoContent(); // 204
        }
    }
}