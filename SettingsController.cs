using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseERP.API.Data;
using EnterpriseERP.API.Services;

namespace EnterpriseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Settings
        [HttpGet]
        public async Task<ActionResult<SystemSetting>> GetSettings()
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                // Seed default settings if none exist
                setting = new SystemSetting
                {
                    CompanyName = "مؤسسة الأفق الذكية للتجارة والخدمات اللوجستية",
                    CompanyLogoUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150&auto=format&fit=crop&q=80",
                    TaxNumber = "310458923100003",
                    CurrencySymbol = "ج.م",
                    Phone = "+20 100 123 4567",
                    Email = "info@alofoq-erp.com",
                    Address = "القاهرة، جمهورية مصر العربية",
                    CommercialRegister = "1049283"
                };
                _context.SystemSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            return setting;
        }

        // 2. PUT: api/Settings
        [HttpPut]
        [HttpPost]
        public async Task<ActionResult<SystemSetting>> UpdateSettings([FromBody] SystemSetting update)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var setting = await _context.SystemSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SystemSetting();
                _context.SystemSettings.Add(setting);
            }

            setting.CompanyName = string.IsNullOrWhiteSpace(update.CompanyName) ? setting.CompanyName : update.CompanyName.Trim();
            setting.CompanyLogoUrl = update.CompanyLogoUrl;
            setting.TaxNumber = update.TaxNumber;
            setting.CurrencySymbol = string.IsNullOrWhiteSpace(update.CurrencySymbol) ? "ج.م" : update.CurrencySymbol.Trim();
            setting.Phone = update.Phone;
            setting.Email = update.Email;
            setting.Address = update.Address;
            setting.CommercialRegister = update.CommercialRegister;

            await _context.SaveChangesAsync();

            await AuditHelper.LogAsync(
                _context,
                "UPDATE",
                "SystemSettings",
                setting.SettingID.ToString(),
                $"تم تحديث إعدادات وهوية المنشأة: {setting.CompanyName} (الرقم الضريبي: {setting.TaxNumber})"
            );

            return Ok(setting);
        }
    }
}
