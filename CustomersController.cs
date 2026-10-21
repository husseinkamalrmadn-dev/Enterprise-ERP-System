using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers([FromQuery] string? search = null)
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => 
                    c.CustomerName.ToLower().Contains(s) ||
                    (c.Phone != null && c.Phone.Contains(s)) ||
                    (c.Email != null && c.Email.ToLower().Contains(s)) ||
                    (c.TaxNumber != null && c.TaxNumber.Contains(s)));
            }

            return await query.OrderByDescending(c => c.CustomerID).ToListAsync();
        }

        // 2. GET: api/Customers/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid customer ID provided." });
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            return customer;
        }

        // 3. POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (customer.CreatedAt == default)
            {
                customer.CreatedAt = DateTime.Now;
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "Customer",
                customer.CustomerID.ToString(),
                $"تم إضافة عميل جديد: {customer.CustomerName} (هاتف: {customer.Phone ?? "غير محدد"})"
            );

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerID }, customer);
        }

        // 4. PUT: api/Customers/5 - Explicit scalar tracking update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid customer ID provided." });
            }

            var existing = await _context.Customers.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            // Enforce ID alignment and preserve creation timestamp
            customer.CustomerID = id;
            if (customer.CreatedAt == default)
            {
                customer.CreatedAt = existing.CreatedAt;
            }

            // Map scalar fields directly to avoid entity state conflicts
            _context.Entry(existing).CurrentValues.SetValues(customer);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "Customer",
                id.ToString(),
                $"تم تعديل بيانات العميل: {customer.CustomerName}"
            );

            return NoContent(); // 204
        }

        // 5. DELETE: api/Customers/5 - Safe deletion with FK integrity checks
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid customer ID provided." });
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            // Check if active sales orders are attached to this customer
            var hasActiveOrders = await _context.SalesOrders.AnyAsync(o => o.CustomerID == id);
            if (hasActiveOrders)
            {
                return BadRequest(new { message = $"Cannot delete customer #{id} because there are sales orders associated with this account." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "Customer",
                id.ToString(),
                $"تم حذف العميل: {customer.CustomerName}"
            );

            return NoContent(); // 204
        }
    }
}