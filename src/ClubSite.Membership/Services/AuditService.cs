using ClubSite.Membership.Data;
using ClubSite.Membership.Models;

namespace ClubSite.Membership.Services;

public class AuditService(MembershipDbContext context) : IAuditService
{
    public async Task LogAsync(string entityName, int entityId, string action, string? oldValues, string? newValues, string? changedFields, string performedBy)
    {
        var entry = new AuditEntry
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedFields = changedFields,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow
        };

        context.AuditLog.Add(entry);
        await context.SaveChangesAsync();
    }
}