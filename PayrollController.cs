using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PayrollController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Payroll
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayrollResponseDto>>> GetPayroll(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var query = _context.Payroll.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.PaymentDate <= eod);
            }

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                query = query.Where(p => p.EmployeeID == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToLower();
                query = query.Where(p => p.Status.ToLower() == st);
            }

            var records = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.PayrollID).ToListAsync();

            // Load Employee map for rich details
            var empIds = records.Select(r => r.EmployeeID).Distinct().ToList();
            var employeesMap = await _context.Employees
                .Where(e => empIds.Contains(e.EmployeeID))
                .ToDictionaryAsync(e => e.EmployeeID, e => e);

            var result = records.Select(p =>
            {
                employeesMap.TryGetValue(p.EmployeeID, out var emp);
                return new PayrollResponseDto
                {
                    PayrollID = p.PayrollID,
                    EmployeeID = p.EmployeeID,
                    EmployeeName = emp?.FullName ?? $"موظف #{p.EmployeeID}",
                    JobTitle = emp?.JobTitle ?? "-",
                    Department = emp?.Department ?? "-",
                    BasicSalary = emp?.BasicSalary ?? 0m,
                    Bonus = p.Bonus,
                    Deductions = p.Deductions,
                    NetSalary = p.NetSalary,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status,
                    Notes = p.Notes
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                result = result.Where(r => 
                    r.EmployeeName.ToLower().Contains(s) || 
                    r.JobTitle.ToLower().Contains(s) || 
                    r.Department.ToLower().Contains(s) ||
                    (r.Notes != null && r.Notes.ToLower().Contains(s))).ToList();
            }

            return result;
        }

        // 2. GET: api/Payroll/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PayrollResponseDto>> GetPayroll(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف مسير الراتب غير صالح." });
            }

            var record = await _context.Payroll.FindAsync(id);
            if (record == null)
            {
                return NotFound(new { message = $"مسير الراتب #{id} غير موجود." });
            }

            var emp = await _context.Employees.FindAsync(record.EmployeeID);

            return new PayrollResponseDto
            {
                PayrollID = record.PayrollID,
                EmployeeID = record.EmployeeID,
                EmployeeName = emp?.FullName ?? $"موظف #{record.EmployeeID}",
                JobTitle = emp?.JobTitle ?? "-",
                Department = emp?.Department ?? "-",
                BasicSalary = emp?.BasicSalary ?? 0m,
                Bonus = record.Bonus,
                Deductions = record.Deductions,
                NetSalary = record.NetSalary,
                PaymentDate = record.PaymentDate,
                Status = record.Status,
                Notes = record.Notes
            };
        }

        // 3. POST: api/Payroll
        [HttpPost]
        public async Task<ActionResult<Payroll>> PostPayroll([FromBody] CreatePayrollRequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (req.EmployeeID <= 0)
            {
                return BadRequest(new { message = "يرجى اختيار الموظف أولاً." });
            }

            var employee = await _context.Employees.FindAsync(req.EmployeeID);
            if (employee == null)
            {
                return BadRequest(new { message = $"الموظف ذو المعرّف #{req.EmployeeID} غير موجود." });
            }

            var baseSalary = employee.BasicSalary;
            var bonus = req.Bonus < 0 ? 0m : req.Bonus;
            var deductions = req.Deductions < 0 ? 0m : req.Deductions;
            var netSalary = (baseSalary + bonus) - deductions;
            if (netSalary < 0) netSalary = 0;

            var payroll = new Payroll
            {
                EmployeeID = req.EmployeeID,
                PaymentDate = req.PaymentDate == default ? DateTime.Now : req.PaymentDate,
                Bonus = bonus,
                Deductions = deductions,
                NetSalary = netSalary,
                Status = string.IsNullOrWhiteSpace(req.Status) ? "Paid" : req.Status.Trim(),
                Notes = req.Notes
            };

            _context.Payroll.Add(payroll);
            await _context.SaveChangesAsync();

            // Automatically register an Expense JournalEntry if Paid
            if (string.Equals(payroll.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                var journalEntry = new JournalEntry
                {
                    TransactionDate = payroll.PaymentDate,
                    Description = $"صرف راتب الموظف: {employee.FullName} ({employee.JobTitle}) عن شهر {payroll.PaymentDate:yyyy-MM}",
                    Amount = payroll.NetSalary,
                    Type = "Expense",
                    ReferenceNumber = $"PAY-{payroll.PayrollID}",
                    CreatedBy = "Payroll System"
                };
                _context.JournalEntries.Add(journalEntry);
                await _context.SaveChangesAsync();
            }

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "Payroll",
                payroll.PayrollID.ToString(),
                $"تم إصدار مسير راتب للموظف: {employee.FullName} بإجمالي صافي {payroll.NetSalary:N2} ج.م (الحالة: {payroll.Status})"
            );

            return CreatedAtAction(nameof(GetPayroll), new { id = payroll.PayrollID }, payroll);
        }

        // 4. PUT: api/Payroll/5 - Safe scalar update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutPayroll(int id, [FromBody] UpdatePayrollRequest req)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف مسير الراتب غير صالح." });
            }

            var existing = await _context.Payroll.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"مسير الراتب #{id} غير موجود." });
            }

            var empId = req.EmployeeID > 0 ? req.EmployeeID : existing.EmployeeID;
            var employee = await _context.Employees.FindAsync(empId);
            if (employee == null)
            {
                return BadRequest(new { message = $"الموظف ذو المعرّف #{empId} غير موجود." });
            }

            var bonus = req.Bonus >= 0 ? req.Bonus : existing.Bonus;
            var deductions = req.Deductions >= 0 ? req.Deductions : existing.Deductions;
            var netSalary = (employee.BasicSalary + bonus) - deductions;
            if (netSalary < 0) netSalary = 0;

            existing.EmployeeID = empId;
            existing.Bonus = bonus;
            existing.Deductions = deductions;
            existing.NetSalary = netSalary;
            existing.Status = string.IsNullOrWhiteSpace(req.Status) ? existing.Status : req.Status.Trim();
            existing.Notes = req.Notes ?? existing.Notes;
            if (req.PaymentDate != default) existing.PaymentDate = req.PaymentDate;

            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "Payroll",
                id.ToString(),
                $"تم تعديل مسير الراتب #{id} للموظف: {employee.FullName} (صافي الراتب: {existing.NetSalary:N2} ج.م)"
            );

            return NoContent();
        }

        // 5. DELETE: api/Payroll/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePayroll(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف مسير الراتب غير صالح." });
            }

            var record = await _context.Payroll.FindAsync(id);
            if (record == null)
            {
                return NotFound(new { message = $"مسير الراتب #{id} غير موجود." });
            }

            _context.Payroll.Remove(record);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "Payroll",
                id.ToString(),
                $"تم حذف مسير الراتب #{id}"
            );

            return NoContent();
        }
    }

    public class CreatePayrollRequest
    {
        public int EmployeeID { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Bonus { get; set; } = 0;
        public decimal Deductions { get; set; } = 0;
        public string Status { get; set; } = "Paid";
        public string? Notes { get; set; }
    }

    public class UpdatePayrollRequest
    {
        public int EmployeeID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class PayrollResponseDto
    {
        public int PayrollID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "Paid";
        public string? Notes { get; set; }
    }
}
