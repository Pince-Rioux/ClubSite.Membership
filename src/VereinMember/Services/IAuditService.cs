using VereinMember.Models;

namespace VereinMember.Services;

public interface IAuditService
{
    Task LogAsync(string entityName, int entityId, string action, string? oldValues, string? newValues, string? changedFields, string performedBy);
}