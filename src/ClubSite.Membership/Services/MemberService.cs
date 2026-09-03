using System.Text.Json;
using System.Text.Json.Serialization;
using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Services;

public class MemberService(MembershipDbContext context, IAuditService auditService) : IMemberService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public async Task<Member?> GetByIdAsync(int id)
    {
        return await context.Members
            .Include(m => m.Department)
            .Include(m => m.MembershipFee)
            .Include(m => m.GuardianMember)
            .Include(m => m.FamilyGroup)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Member>> GetAllAsync()
    {
        return await context.Members
            .Include(m => m.Department)
            .Include(m => m.MembershipFee)
            .AsNoTracking()
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<(List<Member> Items, int TotalCount)> SearchAsync(
        string? searchTerm, MemberStatus? status, int? departmentId,
        FeeCategory? feeCategory, int page, int pageSize)
    {
        IQueryable<Member> query = context.Members
            .Include(m => m.Department)
            .Include(m => m.MembershipFee)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.FirstName.ToLower().Contains(term) ||
                m.LastName.ToLower().Contains(term) ||
                m.Email.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        if (feeCategory.HasValue)
        {
            query = query.Where(m => m.MembershipFee != null && m.MembershipFee.Category == feeCategory.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Member> CreateAsync(Member member, string createdBy)
    {
        // Load fee to validate category constraints
        var fee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == member.MembershipFeeId)
            ?? throw new InvalidOperationException("Selected membership fee not found.");

        // Validate based on fee category
        switch (fee.Category)
        {
            case FeeCategory.Youth:
                if (member.GuardianMemberId == null)
                    throw new InvalidOperationException("Youth member must have a guardian.");
                var guardianFee = await context.MembershipFees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == context.Members
                        .Where(m => m.Id == member.GuardianMemberId)
                        .Select(m => m.MembershipFeeId)
                        .FirstOrDefault());
                if (guardianFee == null || guardianFee.Category != FeeCategory.Adult)
                    throw new InvalidOperationException("Guardian must be an Adult member.");
                break;

            case FeeCategory.Family:
                if (member.FamilyGroupId == null)
                    throw new InvalidOperationException("Family member must belong to a family group.");
                break;

            case FeeCategory.Adult:
                member.GuardianMemberId = null;
                member.FamilyGroupId = null;
                break;
        }

        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        member.CreatedBy = createdBy;
        member.JoinedAt = DateTime.UtcNow;

        context.Members.Add(member);
        await context.SaveChangesAsync();

        await auditService.LogAsync(
            nameof(Member), member.Id, "Created",
            null, member.ToJson(),
            nameof(Member), createdBy);

        return member;
    }

    public async Task<List<Member>> CreateFamilyAsync(Member mainMember, List<Member> familyMembers, string createdBy)
    {
        var fee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == mainMember.MembershipFeeId)
            ?? throw new InvalidOperationException("Selected membership fee not found.");

        if (fee.Category != FeeCategory.Family)
            throw new InvalidOperationException("CreateFamilyAsync requires FeeCategory.Family.");

        var allMembers = new List<Member> { mainMember };
        allMembers.AddRange(familyMembers);

        if (allMembers.Count < 2)
            throw new InvalidOperationException("A family group requires at least 2 members.");

        var now = DateTime.UtcNow;

        // Create family group
        var group = new FamilyGroup
        {
            Name = $"Familie {mainMember.LastName}",
            CreatedAt = now
        };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        // Create all members with the same FamilyGroupId
        foreach (var member in allMembers)
        {
            member.FamilyGroupId = group.Id;
            member.CreatedAt = now;
            member.UpdatedAt = now;
            member.CreatedBy = createdBy;
            member.JoinedAt = now;
            member.Status = MemberStatus.Active;
            member.GuardianMemberId = null; // Family category cannot be Youth

            context.Members.Add(member);
        }

        await context.SaveChangesAsync();

        // Audit each member creation
        foreach (var member in allMembers)
        {
            await auditService.LogAsync(
                nameof(Member), member.Id, "Created",
                null, member.ToJson(),
                nameof(Member), createdBy);
        }

        return allMembers;
    }

    public async Task<Member> UpdateAsync(Member member)
    {
        var existing = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == member.Id);

        if (existing == null)
        {
            throw new KeyNotFoundException($"Member with Id {member.Id} not found.");
        }

        // Load fee to validate category constraints
        var fee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == member.MembershipFeeId)
            ?? throw new InvalidOperationException("Selected membership fee not found.");

        // Validate based on fee category
        switch (fee.Category)
        {
            case FeeCategory.Youth:
                if (member.GuardianMemberId == null)
                    throw new InvalidOperationException("Youth member must have a guardian.");
                var guardianFee = await context.MembershipFees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == context.Members
                        .Where(m => m.Id == member.GuardianMemberId)
                        .Select(m => m.MembershipFeeId)
                        .FirstOrDefault());
                if (guardianFee == null || guardianFee.Category != FeeCategory.Adult)
                    throw new InvalidOperationException("Guardian must be an Adult member.");
                break;

            case FeeCategory.Family:
                if (member.FamilyGroupId == null)
                    throw new InvalidOperationException("Family member must belong to a family group.");
                break;

            case FeeCategory.Adult:
                member.GuardianMemberId = null;
                member.FamilyGroupId = null;
                break;
        }

        var oldValues = existing.ToJson();
        member.UpdatedAt = DateTime.UtcNow;

        context.Members.Update(member);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The record was modified by another user. Please reload and try again.");
        }

        await auditService.LogAsync(
            nameof(Member), member.Id, "Updated",
            oldValues, member.ToJson(),
            string.Empty,
            member.CreatedBy);

        return member;
    }

    public async Task<bool> SoftDeleteAsync(int id, string performedBy)
    {
        // If member is in a family group, check group doesn't drop below 2
        var member = await context.Members.FindAsync(id);
        if (member == null) return false;

        if (member.FamilyGroupId.HasValue)
        {
            var familyCount = await context.Members
                .CountAsync(m => m.FamilyGroupId == member.FamilyGroupId
                                 && m.Status != MemberStatus.Former
                                 && m.Id != id);
            if (familyCount < 2)
                throw new InvalidOperationException("Cannot remove member: family group would have fewer than 2 active members.");
        }

        // Check for dependent Youth members
        var dependents = await context.Members
            .AnyAsync(m => m.GuardianMemberId == id && m.Status != MemberStatus.Former);
        if (dependents)
            throw new InvalidOperationException("Member cannot be deactivated because they are guardian for active Youth members. Re-assign first.");

        var oldValues = member.ToJson();
        member.LeftAt = DateTime.UtcNow;
        member.Status = MemberStatus.Former;
        member.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await auditService.LogAsync(
            nameof(Member), member.Id, "SoftDeleted",
            oldValues, member.ToJson(),
            nameof(Member.LeftAt) + "," + nameof(Member.Status),
            performedBy);

        return true;
    }

    public async Task<List<Member>> GetGuardianCandidatesAsync()
    {
        return await context.Members
            .Include(m => m.MembershipFee)
            .Where(m => m.MembershipFee != null
                        && m.MembershipFee.Category == FeeCategory.Adult
                        && m.Status == MemberStatus.Active)
            .AsNoTracking()
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task<List<Member>> GetDependentsAsync(int guardianId)
    {
        return await context.Members
            .Include(m => m.MembershipFee)
            .Where(m => m.GuardianMemberId == guardianId)
            .AsNoTracking()
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();
    }

    public async Task ErasureAsync(int id, string performedBy)
    {
        // Check dependent Youth members
        var dependents = await context.Members
            .AnyAsync(m => m.GuardianMemberId == id && m.Status != MemberStatus.Former);
        if (dependents)
            throw new InvalidOperationException("Cannot erase: member is guardian of active Youth members.");

        var member = await context.Members.FindAsync(id)
            ?? throw new KeyNotFoundException($"Member {id} not found.");

        var oldValues = member.ToJson();

        // Null out personal data (right to erasure keeps anonymous audit reference)
        member.FirstName = "[erased]";
        member.LastName = "[erased]";
        member.Email = $"[erased-{member.Id}]@verein.de";
        member.Phone = null;
        member.BirthDate = DateTime.MinValue;
        member.Street = null;
        member.City = null;
        member.ZipCode = null;
        member.Country = null;
        member.BankIban = null;
        member.BankBic = null;
        member.Notes = null;
        member.GuardianMemberId = null;
        member.FamilyGroupId = null;
        member.DepartmentId = null;
        member.Status = MemberStatus.Former;
        member.LeftAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        await auditService.LogAsync(
            nameof(Member), member.Id, "Erasure",
            oldValues, member.ToJson(),
            nameof(Member), performedBy);
    }

    public async Task<List<FamilyGroup>> GetAllFamilyGroupsAsync()
    {
        return await context.FamilyGroups
            .Include(g => g.Members)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<FamilyGroup?> GetFamilyGroupByIdAsync(int id)
    {
        return await context.FamilyGroups
            .Include(g => g.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<FamilyGroup> CreateFamilyGroupAsync(string? name, string createdBy)
    {
        var group = new FamilyGroup
        {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        return group;
    }

    public async Task<FamilyGroup> AddMemberToFamilyGroupAsync(int memberId, int familyGroupId)
    {
        var member = await context.Members
            .Include(m => m.MembershipFee)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Member with Id {memberId} not found.");

        var group = await context.FamilyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == familyGroupId)
            ?? throw new KeyNotFoundException($"Family group with Id {familyGroupId} not found.");

        if (member.MembershipFee?.Category != FeeCategory.Family)
            throw new InvalidOperationException("Only Family-category members can be added to a family group.");

        member.FamilyGroupId = familyGroupId;
        await context.SaveChangesAsync();

        return group;
    }

    public async Task<FamilyGroup> RemoveMemberFromFamilyGroupAsync(int memberId)
    {
        var member = await context.Members
            .Include(m => m.FamilyGroup)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Member with Id {memberId} not found.");

        var group = member.FamilyGroup
            ?? throw new InvalidOperationException("Member is not assigned to any family group.");

        // Check min 2 members
        var activeCount = await context.Members
            .CountAsync(m => m.FamilyGroupId == group.Id && m.Id != memberId);
        if (activeCount < 2)
            throw new InvalidOperationException("Family group must have at least 2 members. Remove the entire group instead.");

        member.FamilyGroupId = null;
        await context.SaveChangesAsync();

        return group;
    }

    public async Task<bool> DeleteFamilyGroupAsync(int id)
    {
        var group = await context.FamilyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return false;

        if (group.Members.Any(m => m.Status == MemberStatus.Active))
            throw new InvalidOperationException("Cannot delete a family group that still has active members.");

        context.FamilyGroups.Remove(group);
        await context.SaveChangesAsync();
        return true;
    }
}

internal static class MemberExtensions
{
    public static string ToJson(this Member member)
    {
        return System.Text.Json.JsonSerializer.Serialize(member, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });
    }
}