using ClubSite.Membership.Models;

namespace ClubSite.Membership.Services;

public interface IMemberService
{
    Task<Member?> GetByIdAsync(int id);
    Task<List<Member>> GetAllAsync();
    Task<(List<Member> Items, int TotalCount)> SearchAsync(
        string? searchTerm, MemberStatus? status, int? departmentId,
        FeeCategory? feeCategory, int page, int pageSize);
    Task<Member> CreateAsync(Member member, string createdBy);
    Task<Member> UpdateAsync(Member member);
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