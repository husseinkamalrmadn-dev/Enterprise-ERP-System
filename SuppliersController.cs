using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SuppliersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers([FromQuery] string? search = null)
        {
            var query = _context.Suppliers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(sup => 
                    sup.SupplierName.ToLower().Contains(s) ||
                    (sup.ContactPerson != null && sup.ContactPerson.ToLower().Contains(s)) ||
                    (sup.Phone != null && sup.Phone.Contains(s)) ||
                    (sup.Email != null && sup.Email.ToLower().Contains(s)));
            }

            return await query.OrderByDescending(s => s.SupplierID).ToListAsync();
        }

        // 2. GET: api/Suppliers/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المورد غير صالح." });
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = $"المورد ذو المعرّف #{id} غير موجود." });
            }

            return supplier;
        }

        // 3. POST: api/Suppliers
        [HttpPost]
        public async Task<ActionResult<Supplier>> PostSupplier(Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                return BadRequest(new { message = "اسم المورد مطلوب إجبارياً." });
            }

            if (supplier.CreatedAt == default)
            {
                supplier.CreatedAt = DateTime.Now;
            }

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "Supplier",
                supplier.SupplierID.ToString(),
                $"تم إضافة مورد جديد: {supplier.SupplierName} (هاتف: {supplier.Phone ?? "غير محدد"})"
            );

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierID }, supplier);
        }

        // 4. PUT: api/Suppliers/5 - Safe scalar update using SetValues()
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutSupplier(int id, Supplier supplier)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المورد غير صالح." });
            }

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
            {
                return BadRequest(new { message = "اسم المورد مطلوب إجبارياً." });
            }

            var existing = await _context.Suppliers.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"المورد ذو المعرّف #{id} غير موجود." });
            }

            supplier.SupplierID = id;
            if (supplier.CreatedAt == default)
            {
                supplier.CreatedAt = existing.CreatedAt;
            }

            _context.Entry(existing).CurrentValues.SetValues(supplier);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "Supplier",
                id.ToString(),
                $"تم تعديل بيانات المورد: {supplier.SupplierName}"
            );

            return NoContent();
        }

        // 5. DELETE: api/Suppliers/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المورد غير صالح." });
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = $"المورد ذو المعرّف #{id} غير موجود." });
            }

            // Check if supplier is linked to purchase orders
            var hasOrders = await _context.PurchaseOrders.AnyAsync(po => po.SupplierID == id);
            if (hasOrders)
            {
                return BadRequest(new { message = $"لا يمكن حذف المورد '{supplier.SupplierName}' لوجود فواتير مشتريات مرتبطة به." });
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "Supplier",
                id.ToString(),
                $"تم حذف المورد: {supplier.SupplierName}"
            );

            return NoContent();
        }
    }
}
