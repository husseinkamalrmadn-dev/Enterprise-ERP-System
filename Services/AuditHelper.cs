using EnterpriseERP.API.Data;

namespace EnterpriseERP.API.Services
{
    public static class AuditHelper
    {
        public static async Task LogAsync(
            AppDbContext context,
            string action,
            string entityName,
            string? entityId,
            string details,
            string performedBy = "System Admin")
        {
            try
            {
                var log = new AuditLog
                {
                    Action = action.ToUpperInvariant(),
                    EntityName = entityName,
                    EntityID = entityId ?? "0",
                    Timestamp = DateTime.Now,
                    PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "System Admin" : performedBy,
                    Details = details
                };

                context.AuditLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never let audit log failure break primary business logic
                Console.WriteLine($"[AuditLog Warning]: {ex.Message}");
            }
        }
    }
}
