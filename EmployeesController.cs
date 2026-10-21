using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees(
            [FromQuery] string? search = null,
            [FromQuery] string? department = null)
        {
            var query = _context.Employees.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(e => e.Department.ToLower() == department.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e => 
                    e.FullName.ToLower().Contains(s) ||
                    e.JobTitle.ToLower().Contains(s) ||
                    e.Department.ToLower().Contains(s) ||
                    (e.Phone != null && e.Phone.Contains(s)) ||
                    (e.Email != null && e.Email.ToLower().Contains(s)));
            }

            return await query.OrderByDescending(e => e.EmployeeID).ToListAsync();
        }

        // 2. GET: api/Employees/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف الموظف غير صالح." });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = $"الموظف #{id} غير موجود." });
            }

            return employee;
        }

        // 3. POST: api/Employees
        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee([FromBody] Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(employee.FullName))
            {
                return BadRequest(new { message = "اسم الموظف مطلوب إجبارياً." });
            }

            if (employee.BasicSalary < 0)
            {
                return BadRequest(new { message = "الراتب الأساسي لا يمكن أن يكون سالباً." });
            }

            if (employee.HireDate == default)
            {
                employee.HireDate = DateTime.Now;
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "Employee",
                employee.EmployeeID.ToString(),
                $"تم إضافة موظف جديد: {employee.FullName} ({employee.JobTitle} - {employee.Department}) براتب {employee.BasicSalary:N2} ج.م"
            );

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeID }, employee);
        }

        // 4. PUT: api/Employees/5 - Safe scalar update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutEmployee(int id, [FromBody] Employee employee)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف الموظف غير صالح." });
            }

            if (string.IsNullOrWhiteSpace(employee.FullName))
            {
                return BadRequest(new { message = "اسم الموظف مطلوب إجبارياً." });
            }

            if (employee.BasicSalary < 0)
            {
                return BadRequest(new { message = "الراتب الأساسي لا يمكن أن يكون سالباً." });
            }

            var existing = await _context.Employees.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"الموظف #{id} غير موجود." });
            }

            employee.EmployeeID = id;
            if (employee.HireDate == default)
            {
                employee.HireDate = existing.HireDate;
            }

            _context.Entry(existing).CurrentValues.SetValues(employee);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "Employee",
                id.ToString(),
                $"تم تحديث بيانات الموظف: {employee.FullName} ({employee.JobTitle})"
            );

            return NoContent();
        }

        // 5. DELETE: api/Employees/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف الموظف غير صالح." });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = $"الموظف #{id} غير موجود." });
            }

            // Check if employee has payroll records
            var hasPayroll = await _context.Payroll.AnyAsync(p => p.EmployeeID == id);
            if (hasPayroll)
            {
                return BadRequest(new { message = $"لا يمكن حذف الموظف '{employee.FullName}' لوجود مسيرات رواتب مرتبطة به. يمكنك إلغاء تفعيله بدلاً من الحذف." });
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "Employee",
                id.ToString(),
                $"تم حذف الموظف: {employee.FullName}"
            );

            return NoContent();
        }
    }
}
