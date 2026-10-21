using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductStockController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductStockController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب كل أرصدة المخازن (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductStock>>> GetProductStock()
        {
            return await _context.ProductStock.ToListAsync();
        }

        // 2. إضافة أو تحديث رصيد منتج في مخزن (POST)
        [HttpPost]
        public async Task<ActionResult<ProductStock>> PostProductStock(ProductStock stock)
        {
            // التحقق هل المنتج موجود مسبقاً في هذا المخزن؟
            var existingStock = await _context.ProductStock
                .FirstOrDefaultAsync(s => s.ProductID == stock.ProductID && s.WarehouseID == stock.WarehouseID);

            if (existingStock != null)
            {
                // إذا كان موجوداً، نقوم بتحديث الكمية
                existingStock.QuantityOnHand += stock.QuantityOnHand;
                await _context.SaveChangesAsync();
                return Ok(existingStock);
            }

            // إذا لم يكن موجوداً، نضيف سجل جديد
            _context.ProductStock.Add(stock);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductStock), new { id = stock.StockID }, stock);
        }
    }
}