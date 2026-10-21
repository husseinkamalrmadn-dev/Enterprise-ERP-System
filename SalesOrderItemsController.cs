using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrderItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesOrderItemsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل بنود أوامر البيع (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderItem>>> GetSalesOrderItems()
        {
            return await _context.SalesOrderItems.ToListAsync();
        }

        // 2. إضافة بند جديد لأمر البيع (POST)
        [HttpPost]
        public async Task<ActionResult<SalesOrderItem>> PostSalesOrderItem(SalesOrderItem item)
        {
            _context.SalesOrderItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSalesOrderItems), new { id = item.OrderItemID }, item);
        }
    }
}