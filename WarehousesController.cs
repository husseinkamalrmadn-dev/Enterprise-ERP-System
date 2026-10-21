using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WarehousesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Warehouses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return await _context.Warehouses.AsNoTracking().ToListAsync();
        }

        // 2. GET: api/Warehouses/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid warehouse ID provided." });
            }

            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new { message = $"Warehouse with ID {id} was not found." });
            }

            return warehouse;
        }

        // 3. POST: api/Warehouses
        [HttpPost]
        public async Task<ActionResult<Warehouse>> PostWarehouse(Warehouse warehouse)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.WarehouseID }, warehouse);
        }

        // 4. PUT: api/Warehouses/5 - Explicit scalar tracking update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutWarehouse(int id, Warehouse warehouse)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid warehouse ID provided." });
            }

            var existing = await _context.Warehouses.FindAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"Warehouse with ID {id} was not found." });
            }

            warehouse.WarehouseID = id;

            // Map scalar fields directly to avoid tracking conflict or missing navigation properties
            _context.Entry(existing).CurrentValues.SetValues(warehouse);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }

        // 5. DELETE: api/Warehouses/5 - Safe deletion with inventory checks
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid warehouse ID provided." });
            }

            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
            {
                return NotFound(new { message = $"Warehouse with ID {id} was not found." });
            }

            // Check if warehouse holds active stock
            var hasActiveStock = await _context.ProductStock.AnyAsync(ps => ps.WarehouseID == id && ps.QuantityOnHand > 0);
            if (hasActiveStock)
            {
                return BadRequest(new { message = $"Cannot delete warehouse #{id} because it currently contains active inventory." });
            }

            // Clean up empty stock references if any
            var emptyStocks = await _context.ProductStock.Where(ps => ps.WarehouseID == id).ToListAsync();
            if (emptyStocks.Count > 0)
            {
                _context.ProductStock.RemoveRange(emptyStocks);
            }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}