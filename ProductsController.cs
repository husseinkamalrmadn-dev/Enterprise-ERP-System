using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? search = null)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => 
                    p.ProductName.ToLower().Contains(s) || 
                    p.SKU.ToLower().Contains(s));
            }

            return await query.OrderByDescending(p => p.ProductID).ToListAsync();
        }

        // 2. GET: api/Products/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المنتج غير صالح." });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"المنتج #{id} غير موجود." });
            }

            return product;
        }

        // 3. POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Stock level and pricing validation
            if (product.StockQuantity < 0)
            {
                return BadRequest(new { message = "كمية المخزون لا يمكن أن تكون سالبة." });
            }

            if (product.ReorderLevel < 0)
            {
                return BadRequest(new { message = "حد إعادة الطلب لا يمكن أن يكون سالباً." });
            }

            if (product.UnitPrice < 0 || product.CostPrice < 0)
            {
                return BadRequest(new { message = "أسعار المنتجات لا يمكن أن تكون سالبة." });
            }

            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                product.SKU = $"PRD-{Random.Shared.Next(1000, 9999)}";
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "Product",
                product.ProductID.ToString(),
                $"تم إنشاء منتج جديد: {product.ProductName} ({product.SKU}) برصيد {product.StockQuantity}"
            );

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, product);
        }

        // 4. PUT: api/Products/5 - Explicit scalar tracking update with validation
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المنتج غير صالح." });
            }

            // Stock level and pricing validation
            if (product.StockQuantity < 0)
            {
                return BadRequest(new { message = "كمية المخزون لا يمكن أن تكون سالبة." });
            }

            if (product.ReorderLevel < 0)
            {
                return BadRequest(new { message = "حد إعادة الطلب لا يمكن أن يكون سالباً." });
            }

            if (product.UnitPrice < 0 || product.CostPrice < 0)
            {
                return BadRequest(new { message = "أسعار المنتجات لا يمكن أن تكون سالبة." });
            }

            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"المنتج #{id} غير موجود." });
            }

            // Align entity ID and preserve SKU if not supplied
            product.ProductID = id;
            if (string.IsNullOrWhiteSpace(product.SKU))
            {
                product.SKU = existing.SKU;
            }

            // Map scalar fields directly to avoid tracking conflict
            _context.Entry(existing).CurrentValues.SetValues(product);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "Product",
                id.ToString(),
                $"تم تحديث بيانات المنتج: {product.ProductName} ({product.SKU})"
            );

            return NoContent();
        }

        // 5. GET: api/Products/low-stock - Query products with low stock alerts
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<Product>>> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity < threshold || p.StockQuantity <= p.ReorderLevel)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }

        // 6. POST: api/Products/5/adjust-stock - Direct stock quantity adjustment
        [HttpPost("{id:int}/adjust-stock")]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المنتج غير صالح." });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"المنتج #{id} غير موجود." });
            }

            if (product.StockQuantity + request.Adjustment < 0)
            {
                return BadRequest(new { message = $"الكمية المتاحة في المخزون ({product.StockQuantity}) غير كافية لخصم ({Math.Abs(request.Adjustment)})." });
            }

            var oldStock = product.StockQuantity;
            product.StockQuantity += request.Adjustment;
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "STOCK_SYNC",
                "Product",
                id.ToString(),
                $"تعديل يدوي للمخزون للمنتج {product.ProductName}: من {oldStock} إلى {product.StockQuantity} (الفرق: {request.Adjustment:+0;-0;0}) - السبب: {request.Reason ?? "تعديل رصيد"}"
            );

            return Ok(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                stockQuantity = product.StockQuantity,
                isLowStock = product.StockQuantity < 10 || product.StockQuantity <= product.ReorderLevel
            });
        }

        // 7. DELETE: api/Products/5 - Safe deletion with inventory and order check
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف المنتج غير صالح." });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"المنتج #{id} غير موجود." });
            }

            // Check if product is referenced in sales order line items
            var hasOrderItems = await _context.SalesOrderItems.AnyAsync(i => i.ProductID == id);
            if (hasOrderItems)
            {
                return BadRequest(new { message = $"لا يمكن حذف المنتج #{id} لوجوده في بنود أوامر بيع سابقة." });
            }

            // Check if product is referenced in purchase orders
            var hasPurchases = await _context.PurchaseOrders.AnyAsync(p => p.ProductID == id);
            var hasPOItems = await _context.PurchaseOrderItems.AnyAsync(i => i.ProductID == id);
            if (hasPurchases || hasPOItems)
            {
                return BadRequest(new { message = $"لا يمكن حذف المنتج #{id} لوجود فواتير مشتريات مرتبطة به." });
            }

            // Check if active warehouse inventory exists
            var hasStock = await _context.ProductStock.AnyAsync(s => s.ProductID == id && s.QuantityOnHand > 0);
            if (hasStock)
            {
                return BadRequest(new { message = $"لا يمكن حذف المنتج #{id} لوجود رصيد فعلي في المستودعات." });
            }

            // Clean up empty stock records if any
            var emptyStocks = await _context.ProductStock.Where(s => s.ProductID == id).ToListAsync();
            if (emptyStocks.Count > 0)
            {
                _context.ProductStock.RemoveRange(emptyStocks);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "Product",
                id.ToString(),
                $"تم حذف المنتج: {product.ProductName} ({product.SKU})"
            );

            return NoContent();
        }
    }

    public class StockAdjustmentRequest
    {
        public int Adjustment { get; set; }
        public string? Reason { get; set; }
    }
}