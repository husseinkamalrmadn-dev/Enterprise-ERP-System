using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/AuditLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null,
            [FromQuery] string? search = null,
            [FromQuery] int limit = 200)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.Timestamp <= eod);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                var a = action.Trim().ToUpperInvariant();
                query = query.Where(l => l.Action.ToUpper() == a);
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                var e = entityName.Trim().ToLower();
                query = query.Where(l => l.EntityName.ToLower() == e);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => 
                    (l.Details != null && l.Details.ToLower().Contains(s)) ||
                    l.PerformedBy.ToLower().Contains(s) ||
                    l.EntityID.ToLower().Contains(s) ||
                    l.Action.ToLower().Contains(s) ||
                    l.EntityName.ToLower().Contains(s));
            }

            if (limit <= 0 || limit > 1000)
            {
                limit = 200;
            }

            return await query.OrderByDescending(l => l.Timestamp).Take(limit).ToListAsync();
        }

        // 2. GET: api/AuditLogs/Stats
        [HttpGet("Stats")]
        public async Task<ActionResult<object>> GetAuditStats()
        {
            var totalCount = await _context.AuditLogs.CountAsync();
            var today = DateTime.Today;
            var todayCount = await _context.AuditLogs.CountAsync(l => l.Timestamp >= today);

            var actionsBreakdown = await _context.AuditLogs
                .GroupBy(l => l.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                todayCount,
                actionsBreakdown
            });
        }
    }
}
