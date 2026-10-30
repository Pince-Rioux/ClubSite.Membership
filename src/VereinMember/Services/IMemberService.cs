using VereinMember.Models;

namespace VereinMember.Services;

public interface IMemberService
{
    Task<Member?> GetByIdAsync(int id);

    /// <summary>
    /// Finds a member by their email address. Returns null if not found.
    /// Used for self-service profile lookup after login.
    /// </summary>
    Task<Member?> GetByEmailAsync(string email);

    Task<List<Member>> GetAllAsync();
    Task<(List<Member> Items, int TotalCount)> SearchAsync(
        string? searchTerm, MemberStatus? status, int? departmentId,
        FeeCategory? feeCategory, int page, int pageSize);
    Task<Member> CreateAsync(Member member, string createdBy);

    /// <summary>
    /// Creates a family group with the main member and additional family members.
    /// FamilyGroup is created automatically; all members get FamilyGroupId set.
    /// Requires at least 2 members (main + at least 1 additional).
    /// </summary>
    Task<List<Member>> CreateFamilyAsync(Member mainMember, List<Member> familyMembers, string createdBy);

    Task<Member> UpdateAsync(Member member);

    /// <summary>
    /// Updates only the fields a member is allowed to edit themselves:
    /// FirstName, LastName, Phone, Street, City, ZipCode, Country, Notes.
    /// Throws if the caller tries to set restricted fields.
    /// </summary>
    Task<Member> UpdateOwnProfileAsync(int id, Member updated, string performedBy);
    Task<bool> SoftDeleteAsync(int id, string performedBy);

    // Guardian methods
    Task<List<Member>> GetGuardianCandidatesAsync();
    Task<List<Member>> GetDependentsAsync(int guardianId);

    /// <summary>
    /// DSGVO right to erasure - hard-deletes personal data while keeping anonymous audit reference.
    /// </summary>
    Task ErasureAsync(int id, string performedBy);

    // Family group methods
    Task<List<FamilyGroup>> GetAllFamilyGroupsAsync();
    Task<FamilyGroup?> GetFamilyGroupByIdAsync(int id);
    Task<FamilyGroup> CreateFamilyGroupAsync(string? name, string createdBy);
    Task<FamilyGroup> AddMemberToFamilyGroupAsync(int memberId, int familyGroupId);
    Task<FamilyGroup> RemoveMemberFromFamilyGroupAsync(int memberId);
    Task<bool> DeleteFamilyGroupAsync(int id);
}