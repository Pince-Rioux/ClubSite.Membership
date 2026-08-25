using ClubSite.Membership.Models;

namespace ClubSite.Membership.Services;

public interface IAuditService
{
    Task LogAsync(string entityName, int entityId, string action, string? oldValues, string? newValues, string? changedFields, string performedBy);
}