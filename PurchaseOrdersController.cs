using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/PurchaseOrders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderResponseDto>>> GetPurchaseOrders(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? supplierId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var query = _context.PurchaseOrders.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(po => po.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // End of day
                var eod = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(po => po.OrderDate <= eod);
            }

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                query = query.Where(po => po.SupplierID == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim().ToLower();
                query = query.Where(po => po.Status.ToLower() == st);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(po => 
                    po.OrderNumber.ToLower().Contains(s) || 
                    (po.Notes != null && po.Notes.ToLower().Contains(s)));
            }

            var orders = await query.OrderByDescending(po => po.PurchaseOrderID).ToListAsync();

            // Fetch suppliers and products map for rich display
            var supplierIds = orders.Select(o => o.SupplierID).Distinct().ToList();
            var suppliersMap = await _context.Suppliers
                .Where(s => supplierIds.Contains(s.SupplierID))
                .ToDictionaryAsync(s => s.SupplierID, s => s.SupplierName);

            var productIds = orders.Where(o => o.ProductID.HasValue).Select(o => o.ProductID!.Value).Distinct().ToList();
            var productsMap = await _context.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

            var result = orders.Select(o => new PurchaseOrderResponseDto
            {
                PurchaseOrderID = o.PurchaseOrderID,
                OrderNumber = o.OrderNumber,
                SupplierID = o.SupplierID,
                SupplierName = suppliersMap.TryGetValue(o.SupplierID, out var sName) ? sName : $"مورد #{o.SupplierID}",
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                ProductID = o.ProductID,
                ProductName = o.ProductID.HasValue && productsMap.TryGetValue(o.ProductID.Value, out var pName) ? pName : null,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                Notes = o.Notes
            }).ToList();

            return result;
        }

        // 2. GET: api/PurchaseOrders/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PurchaseOrderDetailDto>> GetPurchaseOrder(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر الشراء غير صالح." });
            }

            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"أمر الشراء #{id} غير موجود." });
            }

            var supplier = await _context.Suppliers.FindAsync(order.SupplierID);
            var items = await _context.PurchaseOrderItems.Where(i => i.PurchaseOrderID == id).ToListAsync();

            string? productName = null;
            if (order.ProductID.HasValue)
            {
                var prd = await _context.Products.FindAsync(order.ProductID.Value);
                productName = prd?.ProductName;
            }

            return new PurchaseOrderDetailDto
            {
                PurchaseOrderID = order.PurchaseOrderID,
                OrderNumber = order.OrderNumber,
                SupplierID = order.SupplierID,
                SupplierName = supplier?.SupplierName ?? $"مورد #{order.SupplierID}",
                SupplierPhone = supplier?.Phone,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ProductID = order.ProductID,
                ProductName = productName,
                Quantity = order.Quantity,
                UnitPrice = order.UnitPrice,
                Notes = order.Notes,
                Items = items
            };
        }

        // 3. POST: api/PurchaseOrders
        [HttpPost]
        public async Task<ActionResult<PurchaseOrder>> PostPurchaseOrder([FromBody] CreatePurchaseOrderRequest req)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (req.SupplierID <= 0)
            {
                return BadRequest(new { message = "يرجى تحديد المورد أولاً." });
            }

            var supplier = await _context.Suppliers.FindAsync(req.SupplierID);
            if (supplier == null)
            {
                return BadRequest(new { message = $"المورد ذو المعرّف #{req.SupplierID} غير موجود." });
            }

            var order = new PurchaseOrder
            {
                OrderNumber = string.IsNullOrWhiteSpace(req.OrderNumber) 
                    ? $"PO-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}" 
                    : req.OrderNumber.Trim(),
                SupplierID = req.SupplierID,
                OrderDate = req.OrderDate == default ? DateTime.Now : req.OrderDate,
                Status = string.IsNullOrWhiteSpace(req.Status) ? "Confirmed" : req.Status.Trim(),
                ProductID = req.ProductID,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice,
                TotalAmount = req.TotalAmount,
                Notes = req.Notes
            };

            // Calculate TotalAmount if not supplied
            if (order.TotalAmount <= 0 && order.Quantity.HasValue && order.UnitPrice.HasValue)
            {
                order.TotalAmount = order.Quantity.Value * order.UnitPrice.Value;
            }

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            // Automated Stock Sync:
            // "Creating a confirmed Purchase Order must automatically increase the StockQuantity of the corresponding products in the database."
            bool isConfirmed = string.Equals(order.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(order.Status, "Received", StringComparison.OrdinalIgnoreCase);

            var stockChanges = new List<string>();

            if (isConfirmed)
            {
                // Single product attached directly to purchase order
                if (order.ProductID.HasValue && order.Quantity.HasValue && order.Quantity.Value > 0)
                {
                    var product = await _context.Products.FindAsync(order.ProductID.Value);
                    if (product != null)
                    {
                        product.StockQuantity += order.Quantity.Value;
                        stockChanges.Add($"{product.ProductName} (+{order.Quantity.Value})");

                        // Also reflect in warehouse stock if default warehouse exists
                        var defaultStock = await _context.ProductStock.FirstOrDefaultAsync(ps => ps.ProductID == product.ProductID);
                        if (defaultStock != null)
                        {
                            defaultStock.QuantityOnHand += order.Quantity.Value;
                        }
                    }
                }

                // Multiple line items if provided
                if (req.Items != null && req.Items.Count > 0)
                {
                    foreach (var item in req.Items)
                    {
                        var poItem = new PurchaseOrderItem
                        {
                            PurchaseOrderID = order.PurchaseOrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };
                        _context.PurchaseOrderItems.Add(poItem);

                        var prd = await _context.Products.FindAsync(item.ProductID);
                        if (prd != null && item.Quantity > 0)
                        {
                            prd.StockQuantity += item.Quantity;
                            stockChanges.Add($"{prd.ProductName} (+{item.Quantity})");

                            var ps = await _context.ProductStock.FirstOrDefaultAsync(s => s.ProductID == prd.ProductID);
                            if (ps != null)
                            {
                                ps.QuantityOnHand += item.Quantity;
                            }
                        }
                    }
                }

                // Automatically generate JournalEntry for Accounting
                var journalEntry = new JournalEntry
                {
                    TransactionDate = order.OrderDate,
                    Description = $"فاتورة مشتريات #{order.OrderNumber} من {supplier.SupplierName}",
                    Amount = order.TotalAmount,
                    Type = "Purchase",
                    ReferenceNumber = order.OrderNumber,
                    CreatedBy = "System Automated Sync"
                };
                _context.JournalEntries.Add(journalEntry);

                await _context.SaveChangesAsync();
            }

            var stockDetails = stockChanges.Count > 0 
                ? $" | تم زيادة رصيد المخزون لـ: {string.Join(", ", stockChanges)}" 
                : "";

            await AuditHelper.LogAsync(
                _context,
                "CREATE",
                "PurchaseOrder",
                order.PurchaseOrderID.ToString(),
                $"تم إنشاء فاتورة مشتريات #{order.OrderNumber} بقيمة {order.TotalAmount:N2} ج.م (الحالة: {order.Status}){stockDetails}"
            );

            return CreatedAtAction(nameof(GetPurchaseOrder), new { id = order.PurchaseOrderID }, order);
        }

        // 4. PUT: api/PurchaseOrders/5 - Safe scalar update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutPurchaseOrder(int id, [FromBody] UpdatePurchaseOrderRequest req)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر الشراء غير صالح." });
            }

            var existing = await _context.PurchaseOrders.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"أمر الشراء #{id} غير موجود." });
            }

            var oldStatus = existing.Status;
            var newStatus = string.IsNullOrWhiteSpace(req.Status) ? existing.Status : req.Status.Trim();

            existing.SupplierID = req.SupplierID > 0 ? req.SupplierID : existing.SupplierID;
            existing.Status = newStatus;
            existing.TotalAmount = req.TotalAmount >= 0 ? req.TotalAmount : existing.TotalAmount;
            existing.Notes = req.Notes ?? existing.Notes;
            if (req.OrderDate != default) existing.OrderDate = req.OrderDate;
            if (req.ProductID.HasValue) existing.ProductID = req.ProductID;
            if (req.Quantity.HasValue) existing.Quantity = req.Quantity;
            if (req.UnitPrice.HasValue) existing.UnitPrice = req.UnitPrice;

            // Automated Stock Sync on confirmation transition
            bool wasConfirmed = string.Equals(oldStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(oldStatus, "Received", StringComparison.OrdinalIgnoreCase);
            bool isNowConfirmed = string.Equals(newStatus, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(newStatus, "Received", StringComparison.OrdinalIgnoreCase);

            string syncNote = "";
            if (!wasConfirmed && isNowConfirmed && existing.ProductID.HasValue && existing.Quantity.HasValue)
            {
                var prd = await _context.Products.FindAsync(existing.ProductID.Value);
                if (prd != null)
                {
                    prd.StockQuantity += existing.Quantity.Value;
                    syncNote = $" | تم زيادة رصيد مخزون {prd.ProductName} بمقدار ({existing.Quantity.Value}) عند تأكيد الفاتورة.";
                }
            }

            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "PurchaseOrder",
                id.ToString(),
                $"تم تعديل أمر الشراء #{existing.OrderNumber} (الحالة: {newStatus}){syncNote}"
            );

            return NoContent();
        }

        // 5. DELETE: api/PurchaseOrders/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePurchaseOrder(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "معرّف أمر الشراء غير صالح." });
            }

            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"أمر الشراء #{id} غير موجود." });
            }

            // Remove child items
            var items = await _context.PurchaseOrderItems.Where(i => i.PurchaseOrderID == id).ToListAsync();
            if (items.Count > 0)
            {
                _context.PurchaseOrderItems.RemoveRange(items);
            }

            _context.PurchaseOrders.Remove(order);
            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "DELETE",
                "PurchaseOrder",
                id.ToString(),
                $"تم حذف أمر الشراء #{order.OrderNumber} ومحتوياته."
            );

            return NoContent();
        }
    }

    public class CreatePurchaseOrderRequest
    {
        public string? OrderNumber { get; set; }
        public int SupplierID { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Confirmed";
        public int? ProductID { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseOrderItemRequest>? Items { get; set; }
    }

    public class PurchaseOrderItemRequest
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdatePurchaseOrderRequest
    {
        public int SupplierID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public int? ProductID { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderResponseDto
    {
        public int PurchaseOrderID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Confirmed";
        public int? ProductID { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderDetailDto : PurchaseOrderResponseDto
    {
        public string? SupplierPhone { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
    }
}
